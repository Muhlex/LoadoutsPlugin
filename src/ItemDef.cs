namespace LoadoutsPlugin;

public partial class ItemDef
{
	public string Name { get; }
	public string DisplayName { get; }
	public string TypeName { get; }
	public GearSlot GearSlot { get; }
	public int GearSlotPos { get; }
	public int Price { get; }
	public IEnumerable<uint> ExclusionGroups { get; }

	public IReadOnlyList<string> Aliases { get; }
	public SearchTerm AliasesSearchTerm { get; }

	public ItemDef(
		string name, string displayName, string typeName, GearSlot gearSlot,
		int gearSlotPos = -1, int price = -1,
		IEnumerable<uint>? exclusionGroups = null, IEnumerable<string>? extraAliases = null
	)
	{
		Name = name;
		DisplayName = displayName;
		TypeName = typeName;
		GearSlot = gearSlot;
		GearSlotPos = gearSlotPos;
		Price = price;
		ExclusionGroups = exclusionGroups ?? [];

		var aliases = new List<string>([displayName]);
		if (extraAliases != null) aliases.AddRange(extraAliases);
		Aliases = aliases;
		AliasesSearchTerm = new SearchTerm(aliases);
	}

	public override string ToString() => $"{Name} ({DisplayName})";
}
