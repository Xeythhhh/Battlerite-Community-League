using DSharpPlus.Entities;

namespace BCL.Discord.Components.Dashboards;
public partial class QueueTracker
{
    public class QueueTrackerMessage(
        DiscordMessageBuilder? builder = null,
        DiscordMessage? discordMessage = null,
        DateTime? timeout = null)
    {
        public DiscordMessageBuilder? Builder { get; set; } = builder;
        public DiscordMessage? DiscordMessage { get; set; } = discordMessage;
        public DateTime? Timeout { get; set; } = timeout;
    }
}
