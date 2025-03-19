using System.Globalization;

using BCL.Common.Extensions;
using BCL.Core.Services.Abstract;
using BCL.Discord.Attributes.Permissions;
using BCL.Discord.Bot;
using BCL.Discord.Extensions;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace BCL.Discord.Commands.SlashCommands.Test;
[SlashCommand_Admin]
[SlashCommandGroup("Test", "Testcommands", false)]
[SlashModuleLifespan(SlashModuleLifespan.Scoped)]
public partial class TestCommands(IUserRepository userRepository, DiscordEngine discordEngine, IQueueService queueService, IChampionRepository championRepository)
    : ApplicationCommandModule
{
    [SlashCommand("RoleInfo", "Debug role information")]
    public async Task RoleInfo(InteractionContext context)
    {
        IReadOnlyDictionary<ulong, DiscordRole> allRoles = discordEngine.Guild.Roles;
        int index = 0;
        string allRolesInfo = allRoles.OrderByDescending(r => r.Value.Position)
            .Aggregate("", (current, role) =>
            {
                (ulong _, DiscordRole discordRole) = role;
                string value = current +
                            $"`{index:00}` | Position: `{discordRole.Position:00}` | Id: `{discordRole.Id}` | Role: {discordRole.Mention} \n";
                index++;
                return value;
            });

        string content = $"""

                       Roles: {allRoles.Count}
                       =================
                       {allRolesInfo}
                       =================
                       """;

        await context.CreateResponseAsync(content);
    }

    [SlashCommand("TestDuplicateStats", "yes")]
    public async Task TestDuplicateStats(InteractionContext context)
    {
        await context.DeferAsync();

        IEnumerable<Domain.Entities.Users.User> users = userRepository.GetAll();

        IEnumerable<Domain.Entities.Users.User> broken = users.Where(u =>
            u.SeasonStats.Any(s1 =>
                u.SeasonStats.Count(s2 => s2.Season == s1.Season) > 1));

        string content = broken.Aggregate("", (current, next) =>
            $"""
             {current}
             {next.InGameName} | {next.DiscordId}
             {next.SeasonStats.Aggregate("", (c, n) => $"{c}\n> - {n.Season} | {n.Snapshots.Count} | {n.RecordedAt.DiscordTime()}")}
             -----------
             """);

        await context.FollowUpAsync(new DiscordFollowupMessageBuilder()
            .WithContent(content));
    }

    [SlashCommand("TestDump", "yes")]
    public async Task TestDump(InteractionContext context,
        [Option("asdf", "sdasds")] Region region)
    {
        await context.DeferAsync();

        List<Domain.Entities.Users.User> users = userRepository.Get(u => u.Pro && u.Server == region).ToList();

        await using MemoryStream stream = new();
        await using StreamWriter writer = new(stream);
        await writer.WriteAsync(users.Aggregate("", (current, user) =>
            $"""
             {current}
             
             ======================================================================
             {user.InGameName} | {user.DiscordId}
             {user.SeasonStats.Where(s => s.PlayedPro).OrderByDescending(s => s.RecordedAt).Aggregate("", (s, stats) => $"{s}\n    {stats.Season.FormatForDiscordCode(15)} | {stats.LatestSnapshot?.Rating.ToString(CultureInfo.InvariantCulture).FormatForDiscordCode(4, true) ?? "----"} | {(stats.LatestSnapshot?.WinRate_Pro ?? 0).ToString("0.00%").FormatForDiscordCode(7)} | {stats.LatestSnapshot?.GamesPlayed_Pro} Matches")}
             """));
        await writer.FlushAsync();
        stream.Position = 0;

        await context.FollowUpAsync(new DiscordFollowupMessageBuilder()
            .WithContent("Debug info")
            .AddFile($"{region}_{DateTime.UtcNow:yyyyMMddhhmm}.txt", stream));
    }
}
