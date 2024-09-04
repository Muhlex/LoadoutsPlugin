using System.Text;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;

namespace LoadoutsPlugin;

public class Monospace
{
	private static readonly Dictionary<char, string> MonospaceMap = new() {
		{ 'a', "𝚊" },
		{ 'b', "𝚋" },
		{ 'c', "𝚌" },
		{ 'd', "𝚍" },
		{ 'e', "𝚎" },
		{ 'f', "𝚏" },
		{ 'g', "𝚐" },
		{ 'h', "𝚑" },
		{ 'i', "𝚒" },
		{ 'j', "𝚓" },
		{ 'k', "𝚔" },
		{ 'l', "𝚕" },
		{ 'm', "𝚖" },
		{ 'n', "𝚗" },
		{ 'o', "𝚘" },
		{ 'p', "𝚙" },
		{ 'q', "𝚚" },
		{ 'r', "𝚛" },
		{ 's', "𝚜" },
		{ 't', "𝚝" },
		{ 'u', "𝚞" },
		{ 'v', "𝚟" },
		{ 'w', "𝚠" },
		{ 'x', "𝚡" },
		{ 'y', "𝚢" },
		{ 'z', "𝚣" },
		{ 'A', "𝙰" },
		{ 'B', "𝙱" },
		{ 'C', "𝙲" },
		{ 'D', "𝙳" },
		{ 'E', "𝙴" },
		{ 'F', "𝙵" },
		{ 'G', "𝙶" },
		{ 'H', "𝙷" },
		{ 'I', "𝙸" },
		{ 'J', "𝙹" },
		{ 'K', "𝙺" },
		{ 'L', "𝙻" },
		{ 'M', "𝙼" },
		{ 'N', "𝙽" },
		{ 'O', "𝙾" },
		{ 'P', "𝙿" },
		{ 'Q', "𝚀" },
		{ 'R', "𝚁" },
		{ 'S', "𝚂" },
		{ 'T', "𝚃" },
		{ 'U', "𝚄" },
		{ 'V', "𝚅" },
		{ 'W', "𝚆" },
		{ 'X', "𝚇" },
		{ 'Y', "𝚈" },
		{ 'Z', "𝚉" },
		{ '0', "𝟶" },
		{ '1', "𝟷" },
		{ '2', "𝟸" },
		{ '3', "𝟹" },
		{ '4', "𝟺" },
		{ '5', "𝟻" },
		{ '6', "𝟼" },
		{ '7', "𝟽" },
		{ '8', "𝟾" },
		{ '9', "𝟿" },
	};

	public static string Convert(string str)
	{
		var builder = new StringBuilder();
		for (int i = 0; i < str.Length; i++)
		{
			if (MonospaceMap.TryGetValue(str[i], out var mono)) builder.Append(mono);
			else builder.Append(str[i]);
		}
		return builder.ToString();
	}
}
