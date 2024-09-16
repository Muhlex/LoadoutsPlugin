namespace LoadoutsPlugin;

public class Loadout
{
	private SortedSet<ItemDef> AllItems { get; } = new(Comparer<ItemDef>.Create((a, b) =>
		a == b ? 0 : a.OrderIndex < b.OrderIndex ? -1 : 1
	));
	// private Dictionary<string, Dictionary<int, ItemDef>> SlotItems { get; } = [];
	private Dictionary<uint, ItemDef> ExclusionGroupItems { get; } = [];

	public Loadout(IEnumerable<ItemDef>? items = null)
	{
		if (items == null) return;
		foreach (var item in items) SetItem(item);
	}

	public IReadOnlySet<ItemDef> Items => AllItems;

	public void SetItem(ItemDef item)
	{
		if (!AllItems.Add(item)) return;
		Console.WriteLine($"Added {item.Name}. Exclusion groups: {string.Join(", ", item.ExclusionGroups)}");

		foreach (var group in item.ExclusionGroups)
		{
			var oldItem = ExclusionGroupItems.GetValueOrDefault(group);
			if (oldItem != null) AllItems.Remove(oldItem);
			ExclusionGroupItems[group] = item;
		}
	}

	public void RemoveItem(ItemDef item)
	{
		if (!AllItems.Remove(item)) return;

		foreach (var group in item.ExclusionGroups) ExclusionGroupItems.Remove(group);
	}

	// public string FormatPrint(CommandCallingContext callingContext)
	// {
	// 	return
	// }
}
