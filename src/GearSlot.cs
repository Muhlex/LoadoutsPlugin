using CounterStrikeSharp.API.Core;

namespace LoadoutsPlugin;

public partial class GearSlot(gear_slot_t value, char chatColor)
{
	public gear_slot_t Value { get; } = value;
	public char ChatColor { get; } = chatColor;
}
