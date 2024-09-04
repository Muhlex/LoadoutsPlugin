namespace LoadoutsPlugin;

public partial class ItemDef
{
	public string Name { get; }
	public string DisplayName { get; }
	public string TypeName { get; }
	public string? GearSlot { get; }
	public IReadOnlyList<string> Aliases { get; }
	public SearchTerm AliasesSearchTerm { get; }

	public ItemDef(string name, string displayName, string typeName, string? gearSlot, IList<string>? extraAliases)
	{
		Name = name;
		DisplayName = displayName;
		TypeName = typeName;
		GearSlot = gearSlot;

		var aliases = new List<string>([displayName]);
		if (extraAliases != null) aliases.AddRange(extraAliases);
		Aliases = aliases;
		AliasesSearchTerm = new SearchTerm(aliases);
	}

	public override string ToString() => $"{Name} ({string.Join(", ", Aliases)})";
}
