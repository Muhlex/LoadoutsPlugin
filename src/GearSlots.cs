using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace LoadoutsPlugin;

public static class GearSlots
{
	static private Dictionary<string, GearSlot> ByName { get; } = new()
	{
		["primary"] = new(gear_slot_t.GEAR_SLOT_FIRST, ChatColors.Lime),
		["secondary"] = new(gear_slot_t.GEAR_SLOT_PISTOL, ChatColors.Olive),
		["melee"] = new(gear_slot_t.GEAR_SLOT_KNIFE, ChatColors.LightYellow),
		["grenade"] = new(gear_slot_t.GEAR_SLOT_GRENADES, ChatColors.LightPurple),
		["item"] = new(gear_slot_t.GEAR_SLOT_C4, ChatColors.LightRed),
		["boost"] = new(gear_slot_t.GEAR_SLOT_BOOSTS, ChatColors.White),
		["utility"] = new(gear_slot_t.GEAR_SLOT_UTILITY, ChatColors.White)
	};
	static private GearSlot InvalidGearSlot { get; } = new(gear_slot_t.GEAR_SLOT_INVALID, ChatColors.Silver);

	public static GearSlot GetByName(string? name)
	{
		if (name == null) return InvalidGearSlot;
		return ByName.TryGetValue(name, out var slot) ? slot : InvalidGearSlot;
	}
}
