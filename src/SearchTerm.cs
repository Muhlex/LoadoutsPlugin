using System.Text.RegularExpressions;

namespace LoadoutsPlugin;

public partial class SearchTerm(IEnumerable<string> aliases)
{
	public SearchTerm(string alias) : this([alias]) { }

	public IEnumerable<string> Aliases { get; } = aliases
		.Select(alias => NonAlphanumericRegex().Replace(alias, ""))
		.Where(alias => alias != "");

	public bool Contains(SearchTerm serachTerm)
	{
		return serachTerm.Aliases.Any(
			searchAlias => Aliases.Any(
				alias => alias.Contains(searchAlias, StringComparison.OrdinalIgnoreCase)
			)
		);
	}

	public bool StartsWith(SearchTerm serachTerm)
	{
		return serachTerm.Aliases.Any(
			searchAlias => Aliases.Any(
				alias => alias.StartsWith(searchAlias, StringComparison.OrdinalIgnoreCase)
			)
		);
	}

	[GeneratedRegex("[^\\w\\d]")]
	private static partial Regex NonAlphanumericRegex();
}
