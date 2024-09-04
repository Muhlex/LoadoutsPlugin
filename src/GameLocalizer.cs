using System.Text.RegularExpressions;

namespace LoadoutsPlugin;

public partial class GameLocalizer()
{
	public static Dictionary<string, string> Localize(IEnumerable<string> keys, Stream kvFile)
	{
		var result = new Dictionary<string, string>();
		// ValveKeyValue cannot handle some values containing quotes, so manually parse the file:
		var reader = new StreamReader(kvFile);
		var regex = KVLineRegex();

		HashSet<string> remainingKeys = new(keys);
		string? line;
		while ((line = reader.ReadLine()) != null && remainingKeys.Count > 0)
		{
			var match = regex.Match(line);
			if (!match.Success) continue;

			foreach (var key in remainingKeys)
			{
				if (!string.Equals(key, match.Groups[1].Value, StringComparison.OrdinalIgnoreCase)) continue;
				result[key] = match.Groups[2].Value;
				remainingKeys.Remove(key);
			}
		}
		return result;
	}

	[GeneratedRegex(@"""(.*)""\s*""(.*)""")]
	private static partial Regex KVLineRegex();
}
