using CounterStrikeSharp.API.Modules.Cvars;

namespace LoadoutsPlugin;

public class ConVars
{
	public readonly ConVar AllowHeavyAssaultSuit = ConVar.Find("mp_weapons_allow_heavyassaultsuit")!;

	public readonly FakeConVar<string> Loadouts = new(
		"sm_loadouts",
		"Loadout rotation. Separate items in one loadout with a comma. Separate loadouts with spaces.",
		""
	);

	// ConVar.StringValue setter is currently broken. Workaround:
	// public static void SetStringValue(ConVar cvar, string value)
	// {
	// 	Server.ExecuteCommand($"{cvar.Name} \"{value}\"");
	// }
}
