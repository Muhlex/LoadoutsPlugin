using System.Text.RegularExpressions;

namespace LoadoutsPlugin;

public partial class SearchTerm(IEnumerable<string> names)
{
	public SearchTerm(string name) : this([name]) { }

	public IReadOnlyList<string> Names { get; } = names.Select(alias => NonAlphanumericRegex().Replace(alias, "")).ToList();

	public bool Contains(SearchTerm serachTerm)
	{
		return serachTerm.Names.Any(
			searchName => Names.Any(
				name => name.Contains(searchName, StringComparison.OrdinalIgnoreCase)
			)
		);
	}

	public bool StartsWith(SearchTerm serachTerm)
	{
		return serachTerm.Names.Any(
			searchName => Names.Any(
				name => name.StartsWith(searchName, StringComparison.OrdinalIgnoreCase)
			)
		);
	}

	[GeneratedRegex("[^\\w\\d]")]
	private static partial Regex NonAlphanumericRegex();
}
