using System.Text;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using SteamDatabase.ValvePak;
using ValveKeyValue;

namespace LoadoutsPlugin;

public class ItemDefs
{
	private IReadOnlyDictionary<string, ItemDef> ByName { get; }
	private IEnumerable<ItemDef> List { get; }
	private IEnumerable<(string, IEnumerable<ItemDef>)> GroupedByType { get; }

	public ItemDefs(IEnumerable<ItemDef> defs)
	{
		var typeNames = new HashSet<string>();
		var defsByName = new Dictionary<string, ItemDef>();
		var defsByType = new Dictionary<string, List<ItemDef>>();
		var slotsByType = new Dictionary<string, HashSet<GearSlot>>();
		var defCountBySlot = new Dictionary<GearSlot, int>();

		var defsSorted = defs.OrderBy(def => def.Price > -1 ? def.Price : int.MaxValue).ThenBy(def => def.DisplayName);
		foreach (var def in defsSorted)
		{
			typeNames.Add(def.TypeName);
			defsByName.Add(def.Name, def);

			if (defsByType.TryGetValue(def.TypeName, out var typeDefs)) typeDefs.Add(def);
			else
			{
				defsByType[def.TypeName] = [def];
				slotsByType[def.TypeName] = [];
			}

			if (def.GearSlot == null) continue;
			slotsByType[def.TypeName].Add(def.GearSlot);
			defCountBySlot[def.GearSlot] = defCountBySlot.GetValueOrDefault(def.GearSlot) + 1;
		}

		ByName = defsByName;

		var typeNamesSorted = typeNames
			.OrderByDescending(type => slotsByType[type].Aggregate(0, (count, slot) => count + defCountBySlot[slot]))
			.ThenByDescending(type => defsByType[type].Count)
			.ThenByDescending(type => defsByType[type].Aggregate(0, (priceSum, def) => def.Price != -1 ? priceSum + def.Price : priceSum));
		GroupedByType = typeNamesSorted.Select(typeName => (typeName, defsByType[typeName].AsEnumerable()));

		var list = new List<ItemDef>();
		foreach (var (_, typeDefs) in GroupedByType)
			foreach (var def in typeDefs)
				list.Add(def);
		List = list;
	}

	public ItemDef? GetByName(string name) => ByName.GetValueOrDefault(name);

	public ItemDef? FindByAlias(string query)
	{
		var searchTerm = new SearchTerm(query);
		var matches = List.Where(def => def.AliasesSearchTerm.Contains(searchTerm));
		var startsWithMatch = matches.FirstOrDefault(def => def.AliasesSearchTerm.StartsWith(searchTerm));
		return startsWithMatch ?? matches.FirstOrDefault();
	}

	public List<string> FormatPrintLines(CommandCallingContext context)
	{
		var lines = new List<string>();
		foreach (var (type, defs) in GroupedByType)
		{
			var line = new StringBuilder();
			if (context == CommandCallingContext.Chat)
			{
				line.Append($"{Chat.NewLine}{ChatColors.Lime}[{ChatColors.Olive}{type}{ChatColors.Lime}]{Chat.NewLine}");
				line.Append(" " + string.Join(" ", defs.Select((def, index) => $"{((index & 1) == 0 ? ChatColors.Default : ChatColors.Silver)}{def.DisplayName}")));
			}
			else
			{
				line.Append($"\n[{type}]\n");
				line.Append(string.Join(", ", defs.Select(def => def.DisplayName)));
			}
			lines.Add(line.ToString());
		};
		return lines;
	}

	private record struct PrefabItemData(string BaseName, gear_slot_t GearSlot, string? DisplayNameKey, string? TypeNameKey);
	public static ItemDefs FromItemsGame(string vpkPath, Config config)
	{
		var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

		using var package = new Package();
		package.Read(vpkPath);

		var itemsGameEntry = package.FindEntry("scripts/items/items_game.txt");
		package.ReadEntry(itemsGameEntry, out byte[] itemsGameBytes);
		var itemsGame = kv.Deserialize(new MemoryStream(itemsGameBytes));


		var items = itemsGame["items"];
		var prefabs = itemsGame["prefabs"];
		KVValue? GetItemProperty(KVValue itemValue, string property)
		{
			var nameTree = property.Split('.');

			var currentPrefab = itemValue;
			while (true)
			{
				KVValue? result = currentPrefab;
				foreach (var name in nameTree)
				{
					result = result[name];
					if (result == null) break;
				}
				if (result != null) return result; // property found
				var nextPrefabName = currentPrefab["prefab"]?.ToString();
				if (nextPrefabName == null) return result; // no child prefab
				currentPrefab = prefabs[nextPrefabName];
				if (currentPrefab == null) return result; // child prefab definition missing
			}
		}

		var includeProperties = Config.CreatePropertiesRegex(config.ItemIncludeProperties);
		var excludeProperties = Config.CreatePropertiesRegex(config.ItemExcludeProperties);

		bool HasProperties(KVValue itemValue, IEnumerable<IEnumerable<(string Name, Regex Regex)>> propertySets)
		{
			return propertySets.Any(set => set.All(p =>
			{
				var propertyValue = GetItemProperty(itemValue, p.Name)?.ToString();
				return propertyValue != null && p.Regex.IsMatch(propertyValue);
			}));
		}

		var filteredItems = new List<(KVValue itemValue, string? displayNameKey, string? typeNameKey)>();
		var localizationKeys = new HashSet<string>();

		foreach (var item in (IEnumerable<KVObject>)items)
		{
			if (includeProperties.Any() && !HasProperties(item.Value, includeProperties)) continue;
			if (HasProperties(item.Value, excludeProperties)) continue;

			var displayNameKey = GetItemProperty(item.Value, "item_name")?.ToString()?.TrimStart('#');
			if (displayNameKey != null) localizationKeys.Add(displayNameKey);
			var typeNameKey = GetItemProperty(item.Value, "item_type_name")?.ToString()?.TrimStart('#');
			if (typeNameKey != null) localizationKeys.Add(typeNameKey);
			filteredItems.Add((item.Value, displayNameKey, typeNameKey));
		}

		var langEnEntry = package.FindEntry("resource/csgo_english.txt");
		package.ReadEntry(langEnEntry, out byte[] langEnBytes);
		var localization = GameLocalizer.Localize(localizationKeys, new MemoryStream(langEnBytes));

		var defs = new List<ItemDef>();
		var exclusionGroupsByItem = config.CreateItemExclusionGroups();
		var slotExclusionGroups = new Dictionary<string, uint>();
		var nextExclusionGroup = (uint)config.MutuallyExclusiveItems.Count;

		foreach (var (itemValue, displayNameKey, typeNameKey) in filteredItems)
		{
			var name = GetItemProperty(itemValue, "name")?.ToString();
			if (name == null) continue;

			var displayName = displayNameKey != null ? localization.GetValueOrDefault(displayNameKey) : null;
			var typeName = typeNameKey != null ? localization.GetValueOrDefault(typeNameKey) : null;

			var priceStr = GetItemProperty(itemValue, "attributes.in game price")?.ToString();
			var price = int.TryParse(priceStr, out var parsedPrice) ? parsedPrice : -1;

			var gearSlotName = GetItemProperty(itemValue, "item_gear_slot")?.ToString();
			var gearSlotPosStr = GetItemProperty(itemValue, "item_gear_slot_position")?.ToString();
			var gearSlotPos = int.TryParse(gearSlotPosStr, out var parsedGearSlotPos) ? parsedGearSlotPos : -1;

			var exclusionGroups = exclusionGroupsByItem.GetValueOrDefault(name, []);

			if (gearSlotName != null && gearSlotPos > -1)
			{
				var slotId = $"{gearSlotName}.{gearSlotPos}";
				var exclusionGroupExists = slotExclusionGroups.TryGetValue(slotId, out var slotExclusionGroup);
				exclusionGroups.Add(exclusionGroupExists ? slotExclusionGroup : nextExclusionGroup);

				if (!exclusionGroupExists)
				{
					slotExclusionGroups[slotId] = nextExclusionGroup;
					nextExclusionGroup++;
				}
			}

			defs.Add(new ItemDef(
				name: name,
				displayName: displayName ?? name,
				typeName: typeName ?? "Other",
				gearSlot: GearSlots.GetByName(gearSlotName),
				gearSlotPos: gearSlotPos,
				price: price,
				exclusionGroups: exclusionGroups,
				extraAliases: config.ItemAliases.GetValueOrDefault(name)
			));
		}

		return new ItemDefs(defs);
	}
}
