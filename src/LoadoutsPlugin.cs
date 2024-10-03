using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
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

	private Loadouts Loadouts { get; set; } = new();
	private Loadout RoundLoadout { get; set; } = new();

	public override void Load(bool hotReload)
	{
		RegisterFakeConVars(ConVars);

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
		Loadouts = Loadouts.FromConVarValue(ItemDefs, ConVars.Loadouts.Value);
		ConVars.Loadouts.ValueChanged += OnConvarLoadoutsChange;
	}

	private void OnConvarLoadoutsChange(object? sender, string value)
	{
		Loadouts = Loadouts.FromConVarValue(ItemDefs, value);
	}

	[GameEventHandler]
	public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
	{
		RoundLoadout = Loadouts.GetRandom();
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

			foreach (var item in RoundLoadout.Items)
			{
				player.GiveNamedItem(item.Name);
				// Hotfix for heavy assault suit not setting correct T model:
				if (item.Name == "item_heavyassaultsuit" && player.Team == CsTeam.Terrorist)
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
		var ctx = command.CallingContext;
		command.ReplyToCommand(FormatOutput($"{Prefix} Available items:", ctx));
		foreach (var message in ItemDefs.FormatPrint(ctx)) command.ReplyToCommand(message);
	}

	[ConsoleCommand("css_loadouts", "View and edit the current starting loadouts.")]
	[ConsoleCommand("css_loadout", "View and edit the current starting loadouts.")]
	[ConsoleCommand("css_lo", "View and edit the current starting loadouts.")]
	public void OnCommandLoadout(CCSPlayerController? player, CommandInfo command)
	{
		var ctx = command.CallingContext;
		var cmdName = FormatCommandName(command);
		var playerName = player != null ? $"{ChatColors.Olive}{player.PlayerName}{ChatColors.Default}" : null;
		bool GetHasPermission()
		{
			string[] permissions = ["@css/cvar", "@loadouts/edit"];
			return permissions.Any(permission => AdminManager.PlayerHasPermissions(player, permission));
		}
		void ReplyRequirePermission()
		{
			command.ReplyToCommand(FormatOutput(
				$"{Prefix} {ChatColors.Red}You are not permitted to edit loadouts.",
				ctx
			));
		}
		var hasPermission = GetHasPermission();

		if (command.ArgCount < 2)
		{
			if (Loadouts.Count == 0)
			{
				command.ReplyToCommand(FormatOutput($"{Prefix} No loadouts defined.", ctx));
			}
			else
			{
				command.ReplyToCommand(FormatOutput($"{Prefix} Current loadout{(Loadouts.Count == 1 ? "" : "s")}:", ctx));
				foreach (var message in Loadouts.FormatPrint(ctx)) command.ReplyToCommand(message);
			}
			if (hasPermission) {
				command.ReplyToCommand(FormatOutput($"{ChatColors.Grey}Use {ChatColors.Default}'{cmdName} help'{ChatColors.Grey} to view loadout editing commands.", ctx));
			}
			return;
		}

		var args = new List<string>();
		for (int i = 1; i < command.ArgCount; i++) args.Add(command.GetArg(i));
		var option = args.First();
		var parameters = args.Skip(1);

		switch (option)
		{
			case "help":
			case "h":
			case "howto":
			case "edit":
			case "?":
				if (!hasPermission) {
					ReplyRequirePermission();
					return;
				}
				command.ReplyToCommand(FormatOutput($"{Prefix} Available loadout commands:", ctx));
				command.ReplyToCommand(FormatOutput([
					$"{cmdName}",
					$"{cmdName} {ChatColors.Olive}add{ChatColors.Default} <item> [item] ... {ChatColors.Grey}(add new loadout)",
					$"{cmdName} {ChatColors.Olive}remove{ChatColors.Default} <loadout # | 'all'> [loadout #] ... {ChatColors.Grey}(remove loadouts of given numbers)",
					$"{cmdName} {ChatColors.Olive}copy{ChatColors.Default} <from loadout #> [to loadout #] {ChatColors.Grey}(copy loadout to new/existing slot)",
					$"{cmdName} {ChatColors.Olive}help{ChatColors.Default} {ChatColors.Grey}(show this help)",
					$"{FormatCommandName(command, "css_items")} {ChatColors.Grey}(list available items)",
				], ctx));
				break;

			case "add":
			case "ad":
			case "a":
			case "create":
			case "new":
				if (!hasPermission) {
					ReplyRequirePermission();
					return;
				}

				var invalidItemAliases = new List<string>();
				var loadout = new Loadout();
				foreach (var partialItemAlias in parameters)
				{
					var item = ItemDefs.FindByAlias(partialItemAlias);
					if (item == null)
					{
						invalidItemAliases.Add(partialItemAlias);
						continue;
					}

					var replacedItems = loadout.SetItem(item);
					if (replacedItems.Count == 0) continue;
					var replacedItemsStr = string.Join(", ", replacedItems.Select(item => item.DisplayName));
					var infoReplacedItem = $"Replaced item{(replacedItems.Count == 1 ? "" : "s")}:";
					command.ReplyToCommand(FormatOutput(
						$"{ChatColors.Grey}{infoReplacedItem} {ChatColors.Red}{replacedItemsStr} ➔ {ChatColors.Default}{item.DisplayName}",
						ctx
					));
				}

				if (invalidItemAliases.Count > 0)
				{
					foreach (var invalidItemAlias in invalidItemAliases)
					{
						command.ReplyToCommand(FormatOutput(
							$"{ChatColors.Grey}Ignoring unknown item: {ChatColors.Red}'{invalidItemAlias}'",
							ctx
						));
					}
					command.ReplyToCommand(FormatOutput(
							$"{ChatColors.Grey}Use {ChatColors.Default}'{FormatCommandName(command, "css_items")}'{ChatColors.Grey} for a list of available items.",
							ctx
						));
				}
				Loadouts.Add(loadout);

				var infoAdded = player != null
					? $"{ChatColors.Olive}{player.PlayerName}{ChatColors.Default} added loadout:"
					: "Loadout added:";
				var infoAddedPrefixed = $"{Prefix} {infoAdded} {loadout.FormatPrint(ctx)}";
				Console.WriteLine(FormatOutput(infoAddedPrefixed, CommandCallingContext.Console));
				Server.PrintToChatAll(FormatOutput(infoAddedPrefixed, CommandCallingContext.Chat));
				break;

			case "remove":
			case "rem":
			case "rm":
			case "r":
			case "delete":
			case "del":
				if (!hasPermission) {
					ReplyRequirePermission();
					return;
				}

				if (Loadouts.Count == 0)
				{
					command.ReplyToCommand(FormatOutput([
						"No loadouts configured.",
						$"{ChatColors.Grey}Use {ChatColors.Default}'{cmdName} add'{ChatColors.Grey} to add one."
					], ctx));
					return;
				}

				List<string> infoRemoveUsage = [
					$"{ChatColors.Grey}Usage: {cmdName} {option} <loadout number> [loadout number] ...",
					$"Remove all loadouts: {cmdName} {option} *",
				];

				if (!parameters.Any())
				{
					command.ReplyToCommand(FormatOutput(
						infoRemoveUsage.Prepend($"{Prefix} No loadout number provided."), ctx
					));
					return;
				};

				var validLoadoutIndices = new SortedSet<int>();
				var invalidLoadoutNumbers = new HashSet<string>();
				foreach (var param in parameters)
				{
					if (param is "all" or "any" or "every" or "clear" or "*")
					{
						Loadouts.Clear();
						var infoCleared = player != null
							? $"{ChatColors.Olive}{player.PlayerName}{ChatColors.Default} removed {ChatColors.Red}all{ChatColors.Default} loadouts."
							: $"{ChatColors.Red}All{ChatColors.Default} loadouts removed.";
						var infoClearedPrefixed = $"{Prefix} {infoCleared}";
						Console.WriteLine(FormatOutput(infoClearedPrefixed, CommandCallingContext.Console));
						Server.PrintToChatAll(FormatOutput(infoClearedPrefixed, CommandCallingContext.Chat));
						return;
					}
					var index = int.TryParse(param, out var i) ? i - 1 : -1;
					if (index >= 0 && index < Loadouts.Count) validLoadoutIndices.Add(index);
					else invalidLoadoutNumbers.Add(param);
				}

				if (invalidLoadoutNumbers.Count > 0)
				{
					var invalidIndicesStr = $"{ChatColors.Red}{string.Join($"{ChatColors.Default}, {ChatColors.Red}", invalidLoadoutNumbers)}";
					command.ReplyToCommand(FormatOutput(
						$"{Prefix} Invalid loadout number(s): {invalidIndicesStr}",
						ctx
					));
				}
				if (validLoadoutIndices.Count == 0) return;

				var removeLoadouts = new Loadouts();
				var removeLoadoutIds = new List<string>();
				foreach (var index in validLoadoutIndices)
				{
					removeLoadouts.Add(Loadouts[index]);
					removeLoadoutIds.Add($"{index + 1}");
				}
				for (var i = validLoadoutIndices.Count - 1; i >= 0; --i) Loadouts.RemoveAt(validLoadoutIndices.ElementAt(i));

				var infoRemoved = player != null
					? $"{ChatColors.Olive}{player.PlayerName}{ChatColors.Default} removed {ChatColors.Red}{removeLoadouts.Count}{ChatColors.Default} loadout{(removeLoadouts.Count == 1 ? "" : "s")}:"
					: $"{ChatColors.Red}{removeLoadouts.Count}{ChatColors.Default} loadout{(removeLoadouts.Count == 1 ? "" : "s")} removed:";
				var infoRemovedPrefixed = $"{Prefix} {infoRemoved}";
				Console.WriteLine(FormatOutput(infoRemovedPrefixed, CommandCallingContext.Console));
				Server.PrintToChatAll(FormatOutput(infoRemovedPrefixed, CommandCallingContext.Chat));
				var validLoadoutNumbers = validLoadoutIndices.Select(i => $"{i + 1}").ToList();
				foreach (var message in removeLoadouts.FormatPrint(CommandCallingContext.Console, validLoadoutNumbers))
					Console.WriteLine(message);
				foreach (var message in removeLoadouts.FormatPrint(CommandCallingContext.Chat, validLoadoutNumbers, (Brackets: ChatColors.Red, Number: ChatColors.LightRed)))
					Server.PrintToChatAll(message);
				break;

			case "copy":
			case "cpy":
			case "cp":
			case "c":
			case "clone":
				if (!hasPermission) {
					ReplyRequirePermission();
					return;
				}
				break;

			default:
				if (int.TryParse(option, out var loadoutNumber) && loadoutNumber > 0)
				{
					if (!hasPermission) {
						ReplyRequirePermission();
						return;
					}
					Console.WriteLine($"Loadout slot {loadoutNumber}");
					break;
				}
				else
				{
					Console.WriteLine($"Unknown option '{option}'");
					break;
				}
		}
	}

	private string Prefix => $"{ChatColors.Gold}[{ChatColors.LightYellow}{ModuleName}{ChatColors.Gold}]{ChatColors.Default}";

	private static string FormatOutput(IEnumerable<string> text, CommandCallingContext context)
	{
		if (context == CommandCallingContext.Chat)
			return $" {string.Join(Chat.NewLine, text).Replace("'", "")}";
		else
			return string.Join('\n', text.Select(StripChatColors));
	}
	private static string FormatOutput(string text, CommandCallingContext context) => FormatOutput([text], context);

	private static string FormatCommandName(CommandInfo command, string? commandName = null)
	{
		var name = commandName ?? command.GetArg(0);
		var isChat = command.CallingContext == CommandCallingContext.Chat;
		if (isChat)
		{
			const string prefix = "css_";
			if (name.StartsWith(prefix)) name = name[prefix.Length..];
		}
		return $"{(isChat ? Chat.Trigger : "")}{name}";
	}

	private static string StripChatColors(string str)
	{
		var builder = new StringBuilder();
		foreach (var chr in str)
		{
			if (char.IsBetween(chr, '\u0001', '\u0010')) continue;
			builder.Append(chr);
		}
		return builder.ToString();
	}
}
