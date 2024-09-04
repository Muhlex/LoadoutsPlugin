using System.Text;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;

namespace LoadoutsPlugin;
public partial class LoadoutsPlugin : BasePlugin, IPluginConfig<Config>
{
	public override string ModuleName => "Loadouts";
	public override string ModuleDescription => "Allow privileged players to select starting items.";
	public override string ModuleAuthor => "murlis";
	public override string ModuleVersion => "1.0.0";

	private ConVars ConVars { get; } = new();
	public Config Config { get; set; } = new();
	private ItemDefs ItemDefs { get; set; } = new([]);

	private List<string[]> Loadouts { get; } = [];
	private string[] RoundLoadout { get; set; } = [];

	public override void Load(bool hotReload)
	{
		RegisterFakeConVars(ConVars);
		ConVars.Loadouts.ValueChanged += OnConvarLoadoutsChange;

		RegisterListener<Listeners.OnServerPrecacheResources>(manifest =>
		{
			// Hotfix for heavy assault suit not setting correct T model:
			Server.PrecacheModel("models/weapons/v_models/arms/phoenix_heavy/v_sleeve_phoenix_heavy.vmdl");
		});
	}

	public void OnConfigParsed(Config config)
	{
		Config = config;
		ItemDefs = ItemDefs.FromItemsGame("../../csgo/pak01_dir.vpk", Config);
	}

	private void OnConvarLoadoutsChange(object? sender, string e)
	{
		Loadouts.Clear();

		var stringSplitOptions = StringSplitOptions.RemoveEmptyEntries & StringSplitOptions.TrimEntries;
		var loadoutStrings = ConVars.Loadouts.Value.Split(' ', stringSplitOptions);
		foreach (var loadoutString in loadoutStrings)
		{
			var itemNames = loadoutString.Split(',', StringSplitOptions.RemoveEmptyEntries);
			Loadouts.Add(itemNames);
		}
	}

	[GameEventHandler]
	public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
	{
		if (Loadouts.Count == 0) RoundLoadout = [];
		else RoundLoadout = Loadouts[Random.Shared.Next(Loadouts.Count)];

		return HookResult.Continue;
	}

	[GameEventHandler]
	public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
	{
		var player = @event.Userid;
		Server.NextFrame(() =>
		{
			if (player == null || !player.IsValid || !player.PawnIsAlive) return;
			if (player.Team is CsTeam.None or CsTeam.Spectator) return;
			var pawn = player.PlayerPawn.Value;
			if (pawn == null || !pawn.IsValid) return;

			player.RemoveWeapons();
			player.GiveNamedItem("weapon_knife");

			foreach (var itemName in RoundLoadout)
			{
				player.GiveNamedItem(itemName);
				// Hotfix for heavy assault suit not setting correct T model:
				if (itemName == "item_heavyassaultsuit" && player.Team == CsTeam.Terrorist)
				{
					pawn.SetModel("characters/models/tm_phoenix_heavy/tm_phoenix_heavy.vmdl");
				}
			}
		});

		return HookResult.Continue;
	}

	[ConsoleCommand("css_items", "Get a list of available items.")]
	public void OnCommandItems(CCSPlayerController? player, CommandInfo command)
	{
		foreach (var line in FormatItemList(command.CallingContext)) command.ReplyToCommand(line);
	}

	[ConsoleCommand("css_loadouts", "Update the current starting items.")]
	[ConsoleCommand("css_loadout", "Update the current starting items.")]
	[RequiresPermissionsOr("@css/cvar", "@loadouts/loadout")]
	public void OnCommandLoadout(CCSPlayerController? player, CommandInfo command)
	{
		if (command.ArgCount < 2)
		{
			command.ReplyToCommand(AddPrefix("No arguments provided. Enter a valid loadout rotation.", command.CallingContext));
			command.ReplyToCommand($"Usage: {ChatTrigger}{FormatCommandName(command)} [item]+[item]... [item]+...");
			command.ReplyToCommand($" {ChatColors.Silver}Example: {ChatTrigger}{FormatCommandName(command)} usp+kevlar ak47+deagle+helmet");
			command.ReplyToCommand($" {ChatColors.Silver}Use {ChatTrigger}{FormatCommandName(command, "css_items")} for a list of available items.");
			return;
		}

		// TODO: Validate identical slots
		// TODO: Print differently on server
		// TODO: Give calling player more feedback than the others

		var name = command.CallingPlayer?.PlayerName;
		Server.PrintToChatAll($" {ChatColors.Olive}{name}{ChatColors.Default} set the loadout rotation to:");

		var loadouts = new List<string>();
		for (int i = 1; i < command.ArgCount; i++)
		{
			var partialAliases = SymbolsRegex().Split(command.GetArg(i)).Where(name => !string.IsNullOrWhiteSpace(name));
			var items = new List<string>();
			var loadoutDisplayStr = new StringBuilder();

			loadoutDisplayStr.Append($" {ChatColors.Silver}[{ChatColors.Default}{Monospace.Convert(i.ToString())}{ChatColors.Silver}] ");
			foreach (var partialAlias in partialAliases)
			{
				var def = ItemDefs.SearchByAlias(partialAlias);
				if (def == null)
				{
					loadoutDisplayStr.Append($"{partialAlias}, ");
					continue;
				}
				loadoutDisplayStr.Append($"{ChatColors.Olive}{def.DisplayName}{ChatColors.Default}, ");
				items.Add(def.Name);
			}
			loadoutDisplayStr.Remove(loadoutDisplayStr.Length - 2, 2);
			Server.PrintToChatAll(loadoutDisplayStr.ToString());

			loadouts.Add(string.Join(',', items));
		}

		ConVars.Loadouts.Value = string.Join(' ', loadouts);
	}

	// [ConsoleCommand("css_weapon", "Update the current starting weapons.")]
	// [RequiresPermissions("@css/cvar")]
	// public void OnCommandWeapon(CCSPlayerController? player, CommandInfo command)
	// {
	// 	var args = new string[command.ArgCount - 1];
	// 	for (int i = 0; i < args.Length; i++) args[i] = command.GetArg(i + 1);

	// 	if (args.Length == 0)
	// 	{
	// 		command.ReplyToCommand(AddPrefix("No arguments provided.", command.CallingContext));
	// 		command.ReplyToCommand($"Usage: {FormatCommandName(command)} <weapon name | 'clear'> ...");
	// 		return;
	// 	}

	// 	var clear = false;
	// 	var invalidWeaponNames = new List<string>();
	// 	var weaponDefs = new Dictionary<gear_slot_t, List<ItemDef>>();
	// 	foreach (var arg in args)
	// 	{
	// 		if (arg == "clear")
	// 		{
	// 			clear = true;
	// 			continue;
	// 		}

	// 		var def = ItemDefs.SearchByAlias(arg);
	// 		if (def == null)
	// 		{
	// 			invalidWeaponNames.Add(arg);
	// 			continue;
	// 		}

	// 		if (weaponDefs.TryGetValue(def.GearSlot, out var defList)) defList.Add(def);
	// 		else weaponDefs.Add(def.GearSlot, [def]);


	// 	}

	// 	// var def = WeaponDefs.SearchByName(command.GetArg(1));
	// 	// if (def == null)
	// 	// {
	// 	var weaponsCmdName = FormatCommandName(command, "css_weapons");
	// 	var weaponsCmdNamePretty = command.CallingContext == CommandCallingContext.Console
	// 		? $"'{weaponsCmdName}'"
	// 		: Monospace.Convert(weaponsCmdName);
	// 	var reply = $"Invalid weapon name. Use command {weaponsCmdNamePretty} for a list of available weapons.";
	// 	command.ReplyToCommand(AddPrefix(reply, command.CallingContext));
	// 	// 	return;
	// 	// }



	// 	// Console.WriteLine(def.DisplayName); // TODO
	// }

	// [ConsoleCommand("css_armor", "Update the current starting armor.")]
	// [RequiresPermissions("@css/cvar")]
	// public void OnCommandArmor(CCSPlayerController? player, CommandInfo command)
	// {
	// 	var allowHeavyAssaultSuit = ConVars.AllowHeavyAssaultSuit.GetPrimitiveValue<bool>();
	// 	string argsUsage = allowHeavyAssaultSuit
	// 		? "<'clear' | 'kevlar' | 'helmet' | 'heavy' | 0 | 1 | 2 | 3>"
	// 		: "<'clear' | 'kevlar' | 'helmet' | 0 | 1 | 2>";
	// 	string usage = $"Usage: {FormatCommandName(command)} {argsUsage}";
	// 	if (command.ArgCount < 2)
	// 	{
	// 		command.ReplyToCommand(AddPrefix("No arguments provided.", command.CallingContext));
	// 		command.ReplyToCommand(usage);
	// 		return;
	// 	}

	// 	var arg = command.GetArg(1);
	// 	var name = command.CallingPlayer?.PlayerName;

	// 	int value;
	// 	string? armorTypeStr = null;

	// 	switch (arg)
	// 	{
	// 		case "0":
	// 		case "clear":
	// 		case "c":
	// 		case "none":
	// 		case "off":
	// 		case "disable":
	// 		case "remove":
	// 			value = 0;
	// 			break;
	// 		case "1":
	// 		case "kevlar":
	// 		case "k":
	// 		case "body":
	// 			value = 1;
	// 			armorTypeStr = "Kevlar only";
	// 			break;
	// 		case "2":
	// 		case "helmet":
	// 		case "h":
	// 		case "head":
	// 			value = 2;
	// 			armorTypeStr = "Kevlar & Helmet";
	// 			break;
	// 		case "3":
	// 		case "has":
	// 		case "as":
	// 		case "heavyassaultsuit":
	// 		case "heavy":
	// 		case "assault":
	// 		case "suit":
	// 			if (!allowHeavyAssaultSuit)
	// 			{
	// 				command.ReplyToCommand(AddPrefix("Heavy Assault Suit is not enabled.", command.CallingContext));
	// 				return;
	// 			}
	// 			value = 3;
	// 			armorTypeStr = "Heavy Assault Suit";
	// 			break;
	// 		default:
	// 			command.ReplyToCommand(AddPrefix($"Invalid armor value: '{arg}'", command.CallingContext));
	// 			command.ReplyToCommand(usage);
	// 			return;
	// 	}

	// 	ConVars.DefaultArmor.Value = value;

	// 	string actionStr = value == 0
	// 		? (name == null ? "Armor disabled" : "disabled armor")
	// 		: (name == null ? "Armor set to" : "set armor to");
	// 	if (name != null) name = $"{name} ";
	// 	if (armorTypeStr != null) armorTypeStr = $" {armorTypeStr}";

	// 	Console.WriteLine(
	// 		AddPrefix($"{name ?? ""}{actionStr}{armorTypeStr ?? ""}.", CommandCallingContext.Console)
	// 	);
	// 	Server.PrintToChatAll(
	// 		AddPrefix($"{ChatColors.Olive}{name}{ChatColors.Default}{actionStr}{ChatColors.Olive}{armorTypeStr ?? ""}{ChatColors.Default}.", CommandCallingContext.Chat)
	// 	);
	// }

	private string ChatTrigger { get; } = CoreConfig.PublicChatTrigger.FirstOrDefault("!");

	private string AddPrefix(string str, CommandCallingContext callingContext)
	{
		if (callingContext == CommandCallingContext.Console) return $"[{ModuleName}] {str}";
		return $" {ChatColors.Gold}[{ChatColors.LightYellow}{ModuleName}{ChatColors.Gold}]{ChatColors.Default} {str}";
	}

	private static string FormatCommandName(CommandInfo command, string? commandName = null)
	{
		const string prefix = "css_";
		var name = commandName ?? command.GetArg(0);
		if (!name.StartsWith(prefix)) return name;
		return command.CallingContext == CommandCallingContext.Console ? name : name[prefix.Length..];
	}

	private List<string> FormatItemList(CommandCallingContext callingContext)
	{
		var lines = new List<string>();
		foreach (var (type, defs) in ItemDefs.GroupedByType)
		{
			lines.Add(" ");
			if (callingContext == CommandCallingContext.Console)
			{
				lines.Add($"[{type}]");
				lines.Add(string.Join(", ", defs.Select(def => def.DisplayName)));
			}
			else
			{
				lines.Add($" {ChatColors.Gold}[{ChatColors.LightYellow}{type}{ChatColors.Gold}]");
				lines.Add(" " + string.Join(" ", defs.Select((def, index) => $"{((index & 1) == 0 ? ChatColors.Default : ChatColors.Silver)}{def.DisplayName}")));
			}
		};
		return lines;
	}

	[GeneratedRegex("[^\\w\\d\\s]")]
	private static partial Regex SymbolsRegex();
}
