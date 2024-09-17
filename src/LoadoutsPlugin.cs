using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
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
		var context = command.CallingContext;
		command.ReplyToCommand(AddPrefix("Available items:", context));
		foreach (var line in ItemDefs.FormatPrintLines(context)) command.ReplyToCommand(line);
	}

	[ConsoleCommand("css_loadouts", "View and edit the current starting loadouts.")]
	[ConsoleCommand("css_loadout", "View and edit the current starting loadouts.")]
	[ConsoleCommand("css_lo", "View and edit the current starting loadouts.")]
	public void OnCommandLoadout(CCSPlayerController? player, CommandInfo command)
	{
		var context = command.CallingContext;
		var isChat = context == CommandCallingContext.Chat;

		if (command.ArgCount < 2)
		{
			if (Loadouts.Count == 0) command.ReplyToCommand(AddPrefix("No loadouts defined.", context));
			else
			{
				command.ReplyToCommand(AddPrefix($"Current loadout{(Loadouts.Count == 1 ? "" : "s")}:", context));
				foreach (var line in Loadouts.FormatPrint(context)) command.ReplyToCommand(line);
			}
			return;
		}

		bool RequirePermission()
		{
			string[] permissions = ["@css/cvar", "@loadouts/edit"];
			var allowed = permissions.Any(permission => AdminManager.PlayerHasPermissions(player, permission));
			if (!allowed) command.ReplyToCommand(AddPrefix("You do not have permission to edit loadouts.", context));
			return allowed;
		}

		var args = new List<string>();
		for (int i = 1; i < command.ArgCount; i++) args.Add(command.GetArg(i));
		var option = args.First();
		var parameters = args.Skip(1);

		switch (option)
		{
			case "add":
			case "ad":
			case "a":
			case "create":
			case "new":
				if (!RequirePermission()) break;
				var loadout = new Loadout();
				foreach (var partialItemAlias in parameters)
				{
					var item = ItemDefs.FindByAlias(partialItemAlias);
					if (item == null)
					{
						command.ReplyToCommand(isChat
							? $" {ChatColors.Grey}Ignoring unknown item: {ChatColors.Red}{partialItemAlias}"
							: $"Ignoring unknown item: '{partialItemAlias}'"
						);
						continue;
					};

					var replacedItems = loadout.SetItem(item);
					if (replacedItems.Count == 0) continue;
					var replacedItemsStr = string.Join(", ", replacedItems.Select(item => item.DisplayName));
					var infoStr = $"Replaced item{(replacedItems.Count == 1 ? "" : "s")}:";
					command.ReplyToCommand(isChat
						? $" {ChatColors.Grey}{infoStr} {ChatColors.Red}{replacedItemsStr} ➔ {ChatColors.Default}{item.DisplayName}"
						: $"{infoStr} {replacedItemsStr} ➔ {item.DisplayName}"
					);
				}
				Loadouts.Add(loadout);
				var loadoutStrConsole = loadout.FormatPrint(CommandCallingContext.Console);
				var announcementConsole = player != null
					? $"{player.PlayerName} added loadout: '{loadoutStrConsole}'"
					: $"Added loadout: '{loadoutStrConsole}'";
				Console.WriteLine(AddPrefix(announcementConsole, CommandCallingContext.Console));
				var loadoutStrChat = loadout.FormatPrint(CommandCallingContext.Chat);
				var announcementChat = player != null
					? $" {ChatColors.Olive}{player.PlayerName}{ChatColors.Default} added loadout: {loadoutStrChat}"
					: $"Added loadout: {loadoutStrChat}";
				Server.PrintToChatAll(AddPrefix(announcementChat, CommandCallingContext.Chat));
				break;

			case "remove":
			case "rem":
			case "rm":
			case "r":
			case "delete":
			case "del":
				if (!RequirePermission()) break;

				if (!parameters.Any())
				{
					// TODO: This and removing everything and feedback
				};

				if (parameters.First() is "all" or "any" or "every" or "clear" or "*")
				{
					Loadouts.Clear();
					Console.WriteLine("Removed all loadouts.");
					break;
				}

				var validRemoveIndices = new List<int>();
				var toRemove = parameters
					.Select(param => int.TryParse(param, out var i) ? (param, i - 1) : (param, -1))
					.Distinct();

				foreach (var (param, index) in toRemove)
				{
					if (index < 0 || index >= Loadouts.Count)
					{
						command.ReplyToCommand(isChat
							? $" {ChatColors.Grey}Not a valid loadout number: {ChatColors.Red}{param}"
							: $"Not a valid loadout number: '{param}'"
						);
						continue;
					}
					validRemoveIndices.Add(index);
				}

				foreach (var index in validRemoveIndices.OrderDescending()) Loadouts.RemoveAt(index);

				var loadoutIndexStr = parameters.FirstOrDefault();
				if (!toRemove.Any())
				{
					Console.WriteLine("No loadout number provided");
					break;
				}
				if (loadoutIndexStr is "all" or "any" or "every" or "*")
				{
					Loadouts.Clear();
					Console.WriteLine("Removed all loadouts.");
					break;
				}

				if (int.TryParse(loadoutIndexStr, out var loadoutIndex) && loadoutIndex > 0 && loadoutIndex <= Loadouts.Count)
				{
					Loadouts.RemoveAt(loadoutIndex - 1);
					Console.WriteLine($"Removed loadout {loadoutIndexStr}");
				}
				else
				{
					Console.WriteLine($"Loadout {loadoutIndexStr} does not exist");
				}
				break;

			case "copy":
			case "cpy":
			case "cp":
			case "c":
			case "clone":
				if (!RequirePermission()) break;
				break;

			default:
				if (int.TryParse(option, out var loadoutSlot) && loadoutSlot > 0)
				{
					if (!RequirePermission()) break;
					Console.WriteLine($"Loadout slot {loadoutSlot}");
					break;
				}
				else
				{
					Console.WriteLine($"Unknown option '{option}'");
					break;
				}
		}

		// TODO: Print differently on server
		// TODO: Give calling player more feedback than the others
	}

	private string ChatTrigger { get; } = CoreConfig.PublicChatTrigger.FirstOrDefault("!");

	private string AddPrefix(string str, CommandCallingContext context)
	{
		if (context == CommandCallingContext.Chat)
			return $" {ChatColors.Gold}[{ChatColors.LightYellow}{ModuleName}{ChatColors.Gold}]{ChatColors.Default} {str}";
		return $"[{ModuleName}] {str}";
	}

	private static string FormatCommandName(CommandInfo command, string? commandName = null)
	{
		const string prefix = "css_";
		var name = commandName ?? command.GetArg(0);
		if (!name.StartsWith(prefix)) return name;
		return command.CallingContext == CommandCallingContext.Chat ? name[prefix.Length..] : name;
	}

	[GeneratedRegex("[^\\w\\d\\s]")]
	private static partial Regex SymbolsRegex();

	[GeneratedRegex("\\s*([\\+\\-]?)\\s*([^\\s]+)")]
	private static partial Regex LoadoutArgsRegex();
}
