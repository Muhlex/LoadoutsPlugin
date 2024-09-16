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

	public string ToConVarValue()
	{
		return string.Join(' ', List.Select(loadout => string.Join(',', loadout.Items)));
	}

	static public Loadouts FromConVarValue(ItemDefs itemDefs, string conVarValue)
	{
		var loadouts = new List<Loadout>();

		var stringSplitOptions = StringSplitOptions.RemoveEmptyEntries & StringSplitOptions.TrimEntries;
		var loadoutStrings = conVarValue.Split(' ', stringSplitOptions);
		foreach (var loadoutString in loadoutStrings)
		{
			var itemNames = loadoutString.Split(',', StringSplitOptions.RemoveEmptyEntries);
			var items = new List<ItemDef>();
			foreach (var itemName in itemNames) {
				var item = itemDefs.GetByName(itemName);
				if (item == null) continue;
				items.Add(item);
			}
			loadouts.Add(new Loadout(items));
		}
		return new Loadouts(loadouts);
	}
}
