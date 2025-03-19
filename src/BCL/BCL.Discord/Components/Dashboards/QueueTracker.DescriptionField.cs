using BCL.Discord.Utils;
using BCL.Domain;

using DSharpPlus.Entities;

namespace BCL.Discord.Components.Dashboards;
public partial class QueueTracker
{
    public static string Status { get; internal set; } = "Queue is currently disabled";

    public static class DescriptionField
    {
        public static string Value => Wrap($"""
            {ANSIColors.Background.Black}{Status}
            """);

        public static DiscordMessageBuilder CreateBuilder() =>
            new DiscordMessageBuilder()
                .WithContent($"# Queue Tracker - {DomainConfig.Season}\n{Value}");
    }
}
