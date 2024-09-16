namespace LoadoutsPlugin;

public partial class ItemDef
{
	public string Name { get; }
	public string DisplayName { get; }
	public string TypeName { get; }
	public int Price { get; }
	public string? GearSlot { get; }
	public IEnumerable<uint> ExclusionGroups { get; }

	public IReadOnlyList<string> Aliases { get; }
	public SearchTerm AliasesSearchTerm { get; }

	public int OrderIndex { get; set; } = -1;

	public ItemDef(
		string name, string displayName, string typeName,
		int price = -1, string? gearSlot = null, IEnumerable<uint>? exclusionGroups = null, IEnumerable<string>? extraAliases = null
		)
	{
		Name = name;
		DisplayName = displayName;
		TypeName = typeName;
		Price = price;
		GearSlot = gearSlot;
		ExclusionGroups = exclusionGroups ?? [];

		var aliases = new List<string>([displayName]);
		if (extraAliases != null) aliases.AddRange(extraAliases);
		Aliases = aliases;
		AliasesSearchTerm = new SearchTerm(aliases);
	}

	public override string ToString() => $"{Name} ({DisplayName})";
}
