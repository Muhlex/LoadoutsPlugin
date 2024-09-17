using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace LoadoutsPlugin;

public class Loadouts
{
	private List<Loadout> List { get; } = [];

	public Loadouts(IEnumerable<Loadout>? loadouts = null)
	{
		if (loadouts == null) return;
		List.AddRange(loadouts);
	}

	public Loadout this[int index]
	{
		get => List[index];
		set => List[index] = value;
	}

	public int Count => List.Count;

	public void Add(Loadout loadout)
	{
		List.Add(loadout);
	}

	public void RemoveAt(int index)
	{
		List.RemoveAt(index);
	}

	public void Clear()
	{
		List.Clear();
	}

	public Loadout GetRandom()
	{
		if (List.Count == 0) return new Loadout();
		else return List[Random.Shared.Next(List.Count)];
	}

	public IEnumerable<string> FormatPrint(CommandCallingContext context)
	{
		var isChat = context == CommandCallingContext.Chat;
		if (!isChat) return [string.Join('\n', List.Select((loadout, i) => $"[{i + 1}] {loadout.FormatPrint(context)}"))];

		var chunkSize = 2;
		return List
			.Chunk(chunkSize)
			.Select((loadouts, i) => string.Join(
				Chat.NewLine,
				loadouts.Select((loadout, j) =>
					$" {ChatColors.Silver}[{ChatColors.Default}{Monospace.Convert($"{i * chunkSize + j + 1}")}{ChatColors.Silver}]{ChatColors.Default} {loadout.FormatPrint(context)}"
				))
			);
	}

	public string ToConVarValue()
	{
		return string.Join(' ', List.Select(loadout => string.Join(',', loadout.Items)));
	}

	static public Loadouts FromConVarValue(ItemDefs itemDefs, string conVarValue)
	{
		Console.WriteLine("Parsing loadouts from convar value");
		var loadouts = new List<Loadout>();

		var stringSplitOptions = StringSplitOptions.RemoveEmptyEntries & StringSplitOptions.TrimEntries;
		var loadoutStrings = conVarValue.Split(' ', stringSplitOptions);
		if (loadoutStrings.Length == 1 && loadoutStrings[0] == "") return new Loadouts(loadouts);

		foreach (var loadoutString in loadoutStrings)
		{
			var itemNames = loadoutString.Split(',', StringSplitOptions.RemoveEmptyEntries);
			var items = new List<ItemDef>();
			foreach (var itemName in itemNames)
			{
				var item = itemDefs.GetByName(itemName);
				if (item == null) continue;
				items.Add(item);
			}
			loadouts.Add(new Loadout(items));
		}
		return new Loadouts(loadouts);
	}
}
