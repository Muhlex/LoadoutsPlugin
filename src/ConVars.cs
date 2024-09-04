using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Cvars.Validators;
using CounterStrikeSharp.API.Modules.Utils;

namespace LoadoutsPlugin;

public class ConVars
{
	// public readonly Dictionary<CsTeam, Dictionary<gear_slot_t, ConVar>> DefaultWeapons = new()
	// {
	// 	[CsTeam.Terrorist] = new()
	// 	{
	// 		[gear_slot_t.GEAR_SLOT_FIRST] = ConVar.Find("mp_t_default_primary")!,
	// 		[gear_slot_t.GEAR_SLOT_PISTOL] = ConVar.Find("mp_t_default_secondary")!,
	// 		[gear_slot_t.GEAR_SLOT_KNIFE] = ConVar.Find("mp_t_default_melee")!,
	// 		[gear_slot_t.GEAR_SLOT_GRENADES] = ConVar.Find("mp_t_default_grenades")!
	// 	},
	// 	[CsTeam.CounterTerrorist] = new()
	// 	{
	// 		[gear_slot_t.GEAR_SLOT_FIRST] = ConVar.Find("mp_ct_default_primary")!,
	// 		[gear_slot_t.GEAR_SLOT_PISTOL] = ConVar.Find("mp_ct_default_secondary")!,
	// 		[gear_slot_t.GEAR_SLOT_KNIFE] = ConVar.Find("mp_ct_default_melee")!,
	// 		[gear_slot_t.GEAR_SLOT_GRENADES] = ConVar.Find("mp_ct_default_grenades")!
	// 	}
	// };

	public readonly ConVar AllowHeavyAssaultSuit = ConVar.Find("mp_weapons_allow_heavyassaultsuit")!;

	public readonly FakeConVar<string> Loadouts = new(
		"sm_loadouts",
		"Loadout rotation. Separate items in one loadout with a comma. Separate loadouts with spaces.",
		""
	);
	// public readonly FakeConVar<int> DefaultArmor = new(
	// 	"sm_loadouts_armor",
	// 	"Default armor value if omitted in loadouts.",
	// 	1,
	// 	ConVarFlags.FCVAR_NONE,
	// 	new RangeValidator<int>(0, 3)
	// );

	// public readonly IReadOnlyDictionary<gear_slot_t, FakeConVar<string>> Weapons = new Dictionary<gear_slot_t, FakeConVar<string>>()
	// {
	// 	[gear_slot_t.GEAR_SLOT_FIRST] = new("sm_weapon_select_primary", "Starting primary. Comma separate multiple values to create a rotation.", ""),
	// 	[gear_slot_t.GEAR_SLOT_PISTOL] = new("sm_weapon_select_secondary", "Starting secondary. Comma separate multiple values to create a rotation.", ""),
	// 	[gear_slot_t.GEAR_SLOT_KNIFE] = new("sm_weapon_select_melee", "Starting melee. Comma separate multiple values to create a rotation.", ""),
	// 	[gear_slot_t.GEAR_SLOT_GRENADES] = new("sm_weapon_select_grenades", "Starting grenades. Comma separate multiple values to create a rotation.", "")
	// };

	// ConVar.StringValue setter is currently broken. Workaround:
	// public static void SetStringValue(ConVar cvar, string value)
	// {
	// 	Server.ExecuteCommand($"{cvar.Name} \"{value}\"");
	// }
}
