using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace LoadoutsPlugin;

public class Loadout
{
	private SortedSet<ItemDef> AllItems { get; } = new(Comparer<ItemDef>.Create((a, b) =>
	{
		if (a == b) return 0;

		if (a.GearSlot.Value == b.GearSlot.Value)
		{
			if (a.GearSlotPos == b.GearSlotPos)
			{
				if (a.Price == b.Price)
				{
					var displayNameCompare = a.DisplayName.CompareTo(b.DisplayName);
					if (displayNameCompare != 0) return displayNameCompare;
					return a.Name.CompareTo(b.Name);
				}
				return a.Price < b.Price ? -1 : 1;
			}
			return a.GearSlotPos < b.GearSlotPos ? -1 : 1;
		}
		return a.GearSlot.Value < b.GearSlot.Value ? -1 : 1;
	}
	));
	private Dictionary<uint, ItemDef> ExclusionGroupItems { get; } = [];

	public Loadout(IEnumerable<ItemDef>? items = null)
	{
		if (items == null) return;
		foreach (var item in items) SetItem(item);
	}

	public IReadOnlySet<ItemDef> Items => AllItems;

	public List<ItemDef> SetItem(ItemDef item)
	{
		if (!AllItems.Add(item)) return [item];

		var oldItems = new List<ItemDef>();
		foreach (var group in item.ExclusionGroups)
		{
			var oldItem = ExclusionGroupItems.GetValueOrDefault(group);
			if (oldItem != null)
			{
				AllItems.Remove(oldItem);
				oldItems.Add(oldItem);
			}
			ExclusionGroupItems[group] = item;
		}
		return oldItems;
	}

	public void RemoveItem(ItemDef item)
	{
		if (!AllItems.Remove(item)) return;

		foreach (var group in item.ExclusionGroups) ExclusionGroupItems.Remove(group);
	}

	public string FormatPrint(CommandCallingContext context)
	{
		var isChat = context == CommandCallingContext.Chat;
		if (AllItems.Count == 0)
		{
			if (isChat) return $"{ChatColors.DarkBlue}<empty>";
			else return "<empty>";
		}
		else
		{
			if (isChat)
				return string.Join(
					$"{ChatColors.Default}, ",
					AllItems.Select(item => $"{item.GearSlot.ChatColor}{item.DisplayName}")
				);
			else
				return string.Join(", ", AllItems.Select(item => item.DisplayName));
		}
	}
}
