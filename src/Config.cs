
using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Core;

namespace LoadoutsPlugin;

public class Config : BasePluginConfig
{
	public List<Dictionary<string, string>> ItemIncludeProperties { get; set; } = [
		new() { ["item_class"] = "^(weapon_)|(item_)" }
	];
	public List<Dictionary<string, string>> ItemExcludeProperties { get; set; } = [
		new() { ["item_class"] = "^weapon_knife$" },
		new() { ["item_class"] = "^weapon_(?!healthshot$)", ["flexible_loadout_slot"] = "^none$" },
		new() { ["item_class"] = "^(item_nvgs)|(item_defuser)$" },
		new() { ["flexible_loadout_category"] = "^(heavy)|(c4)$" },
	];

	public Dictionary<string, List<string>> ItemAliases { get; set; } = new()
	{
		["weapon_deagle"] = ["Deagle"],
		["weapon_elite"] = ["Dual Elites"],
		["weapon_glock"] = ["G18"],
		["weapon_sg556"] = ["Krieg"],
		["weapon_ssg08"] = ["Scout"],
		["weapon_xm1014"] = ["Auto Shotgun"],
		["weapon_hegrenade"] = ["HE Grenade"],
		["weapon_molotov"] = ["Molly"],
		["weapon_incgrenade"] = ["CT Molotov", "CT Molly"],
		["weapon_taser"] = ["Taser"],
		["item_kevlar"] = ["Armor", "1"],
		["item_assaultsuit"] = ["Kevlar Helmet", "Helmet", "2"],
		["item_heavyassaultsuit"] = ["Heavy Armor", "3"],
		["item_defuser"] = ["Defuser"],
		["weapon_healthshot"] = ["Health-Shot"]
	};

	public List<List<string>> MutuallyExclusiveItems { get; set; } = [
		["item_kevlar", "item_assaultsuit", "item_heavyassaultsuit"]
	];

	public Dictionary<string, List<uint>> CreateItemExclusionGroups()
	{
		var groupsByItemName = new Dictionary<string, List<uint>>();
		for (var group = 0; group < MutuallyExclusiveItems.Count; group++)
		{
			foreach (var itemName in MutuallyExclusiveItems[group])
			{
				if (groupsByItemName.TryGetValue(itemName, out var groups)) groups.Add((uint)group);
				else groupsByItemName[itemName] = [(uint)group];
			}
		}
		return groupsByItemName;
	}

	public static IEnumerable<IEnumerable<(string, Regex)>> CreatePropertiesRegex(List<Dictionary<string, string>> properties)
	{
		return properties.Select(list => list.Select(p => (Name: p.Key, Regex: new Regex(p.Value))));
	}
}
