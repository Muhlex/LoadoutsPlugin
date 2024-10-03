using CounterStrikeSharp.API.Core;

namespace LoadoutsPlugin;

public static class Chat
{
	public const char NewLine = '\u2029';
	public static string Trigger { get; } = CoreConfig.PublicChatTrigger.FirstOrDefault("");
}
