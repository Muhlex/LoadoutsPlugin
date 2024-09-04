using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Core;
using SteamDatabase.ValvePak;
using ValveKeyValue;

namespace LoadoutsPlugin;

public class ItemDefs
{
	public IReadOnlyDictionary<string, ItemDef> ByName { get; }
	public IEnumerable<ItemDef> Alphabetical { get; }
	public IEnumerable<(string, IEnumerable<ItemDef>)> GroupedByType { get; }

	public ItemDefs(IEnumerable<ItemDef> defs)
	{
		var defsSorted = defs.OrderBy(def => def.DisplayName);
		Alphabetical = defsSorted;

		var typeNames = new HashSet<string>();
		var defsByType = new Dictionary<string, List<ItemDef>>();
		var defsByName = new Dictionary<string, ItemDef>();
		foreach (var def in defsSorted)
		{
			typeNames.Add(def.TypeName);
			defsByName.Add(def.Name, def);
			if (defsByType.TryGetValue(def.TypeName, out var defList)) defList.Add(def);
			else defsByType.Add(def.TypeName, [def]);
		}

		var typeNamesSorted = typeNames.OrderByDescending(x => defsByType[x].Count);
		ByName = defsByName;
		GroupedByType = typeNamesSorted.Select(typeName => (typeName, defsByType[typeName].AsEnumerable()));
	}

	public ItemDef? SearchByAlias(string query)
	{
		var searchTerm = new SearchTerm(query);
		var matches = Alphabetical.Where(def => def.AliasesSearchTerm.Contains(searchTerm));
		var startsWithMatch = matches.FirstOrDefault(def => def.AliasesSearchTerm.StartsWith(searchTerm));
		return startsWithMatch ?? matches.FirstOrDefault();
	}

	public List<ItemDef> GetFromConVar(string value)
	{
		var result = new List<ItemDef>();
		foreach (var name in value.Split(',', StringSplitOptions.RemoveEmptyEntries))
		{
			var def = ByName.GetValueOrDefault(name.Trim());
			if (def == null) continue;
			result.Add(def);
		}
		return result;
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

		var includeProperties = Config.ParsePropertiesRegex(config.ItemIncludeProperties);
		var excludeProperties = Config.ParsePropertiesRegex(config.ItemExcludeProperties);

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
			var value = item.Value;

			if (includeProperties.Any() && !HasProperties(item.Value, includeProperties)) continue;
			if (HasProperties(item.Value, excludeProperties)) continue;

			var displayNameKey = GetItemProperty(item.Value, "item_name")?.ToString()?.TrimStart('#');
			if (displayNameKey != null) localizationKeys.Add(displayNameKey);
			var typeNameKey = GetItemProperty(item.Value, "item_type_name")?.ToString()?.TrimStart('#');
			if (typeNameKey != null) localizationKeys.Add(typeNameKey);
			filteredItems.Add((value, displayNameKey, typeNameKey));
		}

		var langEnEntry = package.FindEntry("resource/csgo_english.txt");
		package.ReadEntry(langEnEntry, out byte[] langEnBytes);
		var localization = GameLocalizer.Localize(localizationKeys, new MemoryStream(langEnBytes));

		var defs = new List<ItemDef>();
		foreach (var (itemValue, displayNameKey, typeNameKey) in filteredItems)
		{
			var name = itemValue["name"]?.ToString();
			if (name == null) continue;

			var displayName = displayNameKey != null ? localization.GetValueOrDefault(displayNameKey) : null;
			var typeName = typeNameKey != null ? localization.GetValueOrDefault(typeNameKey) : null;

			defs.Add(new ItemDef(
				name: name,
				displayName: displayName ?? name,
				typeName: typeName ?? "Other",
				gearSlot: itemValue["item_gear_slot"]?.ToString(),
				extraAliases: config.ItemAliases.GetValueOrDefault(name)
			));
		}

		return new ItemDefs(defs);
	}
}
