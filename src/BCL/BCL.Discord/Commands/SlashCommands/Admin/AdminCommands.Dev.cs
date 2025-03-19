using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using BCL.Core;
using BCL.Discord.Attributes.Permissions;
using BCL.Discord.Bot;
using BCL.Discord.Components.Dashboards;
using BCL.Discord.Extensions;
using BCL.Domain;
using BCL.Domain.Entities.Analytics;
using BCL.Domain.Entities.Users;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;

namespace BCL.Discord.Commands.SlashCommands.Admin;
[SlashModuleLifespan(SlashModuleLifespan.Scoped)]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
public partial class AdminCommands
{
    [SlashCommand_Dev]
    [SlashCommandGroup("dev", "Yes hi hello", false)]
    [SlashModuleLifespan(SlashModuleLifespan.Scoped)]
    public partial class Dev(
        IMatchRepository matchRepository,
        IUserRepository userRepository,
        IChampionRepository championRepository,
        IAnalyticsRepository analytics,
        DiscordEngine discordEngine) : ApplicationCommandModule
    {
        [SlashCommand("DumpStatsDupeDebugInfo", "yes")]
        public async Task DumpStatsDupeDebugInfo(InteractionContext context)
        {
            await context.CreateResponseAsync("Please wait...");
            await context.DeleteResponseAsync();

            List<User> users = userRepository.GetAll().ToList();
            DiscordMessage feedback = await context.Channel.SendMessageAsync("Finding duplicate stats...");
            DateTime updated = DateTime.UtcNow;
            int index = 0;
            int updatedIndex = 0;
            ProgressBar progress = new(75,
                [(users.Count, "Finding duplicate stats...")]);

            List<User> duplicates = users.Where(u =>
            {
                if ((DateTime.UtcNow - updated) > TimeSpan.FromSeconds(5))
                {
                    updated = DateTime.UtcNow;
                    progress.Add(index - updatedIndex);
                    updatedIndex = index;
                    feedback.ModifyAsync($"{progress}\n{index}/{users.Count}");
                }

                index++;
                return u.SeasonStats.GroupBy(s => s.Season)
                    .Any(g => g.Count() > 1);
            }).ToList();

            await using MemoryStream debugStream = new();
            await using StreamWriter writer = new(debugStream);
            await writer.WriteAsync(duplicates.Aggregate("", (current, user) =>
                $"""
                 {current}
                 ===================================
                 {user.InGameName} | {user.DiscordId}
                 {user.SeasonStats.Aggregate("", (s, stats) => $"{s}     {stats.Id} | {stats.Season} | {stats.RecordedAt} | {stats.Snapshots.Count}")}
                 """));
            await writer.FlushAsync();
            debugStream.Position = 0;

            await feedback.ModifyAsync(new DiscordMessageBuilder()
                .WithContent("Debug info")
                .AddFile($"Debug_Test_{DateTime.UtcNow:yyyyMMddhhmm}.txt", debugStream));
        }

        [SlashCommand("ResetSeason", "Reset the season without changing it")]
        public async Task ResetSeason(InteractionContext context)
        {
            await context.DeferAsync();

            MigrationInfo migrationInfo = analytics.GetMigrationInfo() ?? throw new UnreachableException("MigrationInfo can not be null!");
            DSharpPlus.Interactivity.InteractivityExtension interactivity = context.Client.GetInteractivity();
            string randomUlid = Ulid.NewUlid(DateTimeOffset.UtcNow).ToString();
            await context.Channel.SendMessageAsync(new DiscordMessageBuilder()
                .WithContent($"Are you sure? This can not be undone, it's intended use is at a problematic season start. Type `{randomUlid}` to confirm."));

            DSharpPlus.Interactivity.InteractivityResult<DiscordMessage> response = await interactivity.WaitForMessageAsync(m => m.Author.Id == context.User.Id);

            if (response.TimedOut) return;
            if (response.Result.Content != randomUlid) return;

#pragma warning disable RCS1246 // Use element access
            List<Domain.Entities.Matches.Match> matches = matchRepository.Get(m => m.Season == migrationInfo.Seasons.Last().Label).ToList();
#pragma warning restore RCS1246 // Use element access
            foreach (Domain.Entities.Matches.Match? match in matches) matchRepository.Delete(match.Id);
            await matchRepository.SaveChangesAsync();

            List<User> users = userRepository.GetAll().ToList();
            foreach (User? user in users) user.CurrentSeasonStats.Season = $"Deleted-{user.CurrentSeasonStats.Season}-{DateTime.UtcNow}";
            await userRepository.SaveChangesAsync();

            string content = $"Removed {matches.Count} matches and reset stats for {users.Count} users. (Triggered by {context.User.Mention})";
            await context.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent(content));

            await discordEngine.Log(content);
        }

        [SlashCommand("StartNewSeason", "This can only be used on a fresh new season.")]
        public async Task StartNewSeason(InteractionContext context,
            [Option("SeasonName", "Name of new season")] string seasonName,
            [Option("HardReset", "Hard reset ratings to default or soft reset?")] bool hardReset = false)
        {
            if (seasonName.Contains(';')) throw new ArgumentException("SeasonName cannot contain ';'.");
            await context.CreateResponseAsync($"Starting new season `{seasonName}`...");

            MigrationInfo migrationInfo = analytics.GetMigrationInfo() ?? throw new UnreachableException("MigrationInfo can not be null!");
            MigrationInfo._Season lastSeason = migrationInfo.Seasons[^1];

            DiscordMessage confirmationPrompt = await context.Channel
                .SendMessageAsync($"""
                                              Are you sure you want to start a new season called `{seasonName}`?
                                              Type the name of the current season to confirm ('{lastSeason.Label}')
                                              """);
            DSharpPlus.Interactivity.InteractivityExtension interactivity = context.Client.GetInteractivity();
            DSharpPlus.Interactivity.InteractivityResult<DiscordMessage> response = await interactivity.WaitForMessageAsync(m => m.Author == context.User);
            if (response.TimedOut || !response.Result.Content.Equals(lastSeason.Label, StringComparison.CurrentCultureIgnoreCase))
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Aborted"));
                return;
            }
            await confirmationPrompt.DeleteAsync();

            string ratingResetFeedback = await ResetRatings(hardReset);
            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent(ratingResetFeedback));

            DomainConfig.Season = seasonName;
            migrationInfo.AddSeason(seasonName, DateTime.UtcNow, 0);

            foreach (User user in userRepository.GetAll())
            {
                user.WinStreak = 0;
                user.WinStreak_Pro = 0;
                user.WinStreak_Standard = 0;

                user.CurrentSeasonStats.Snapshots.Add(
                    new StatsSnapshot
                    {
                        Rating = user.Rating,
                        Rating_Standard = user.Rating_Standard
                    });
            }

            await userRepository.SaveChangesAsync();
            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Started season `{seasonName}` using `{(hardReset ? "Hard" : "Soft")}` reset."));

            DiscordMessage feedback = await context.Channel.SendMessageAsync("Updating Queue Tracker...");
            await discordEngine.QueueTracker.Refresh(feedback);
            await feedback.DeleteAsync(10);
        }

        [SlashCommand("SetStatus", "Set bot status")]
        public async Task SetStatus(InteractionContext context,
            [Option("ActivityType", "Bot activity")] ActivityType activity,
            [Option("Status", "Bot status")] string status)
        {
            if (activity is ActivityType.Custom)
            {
                await context.CreateResponseAsync("Bot can not have a custom status.");
                return;
            }
            QueueTracker.Status = status;
            _ = discordEngine.QueueTracker.Refresh(target: QueueTracker.QueueTrackerField.Description);
            await context.Client.UpdateStatusAsync(new DiscordActivity(status, activity));
            await context.CreateResponseAsync($"Bot status set to **{activity}** ` {status}`");
        }

        [SlashCommand("ResetProMemberships", "Set every player's membership to standard")]
        public async Task ResetProMemberships(InteractionContext context)
        {
            await context.CreateResponseAsync("Resetting pro memberships ...");

            foreach (User user in userRepository.Get(u => u.Pro)) user.Pro = false;
            await userRepository.SaveChangesAsync();

            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent(":+1:"));

            DiscordMessage feedback = await context.Channel.SendMessageAsync("Updating Queue Tracker...");
            await discordEngine.QueueTracker.Refresh(feedback);
            await feedback.DeleteAsync(10);
        }

        private async Task<string> ResetRatings(bool hardReset)
        {
            List<User> users = userRepository.GetAll().ToList();

            foreach (User? user in users)
            {
                user.Rating = DomainConfig.DefaultRating;

                user.Rating_Standard = (double)(hardReset
                    ? DomainConfig.DefaultRating
                    : Math.Round(user.Rating_Standard + ((DomainConfig.DefaultRating - user.Rating_Standard) / 2)));

                user.PlacementGamesRemaining = CoreConfig.Queue.PlacementGames;
                user.PlacementGamesRemaining_Standard = CoreConfig.Queue.PlacementGames;
            }

            await userRepository.SaveChangesAsync();
            string content = $"Ratings reset for {users.Count} users.({(hardReset ? "Hard" : "Soft")} Reset)";
            await discordEngine.Log(content);
            return content;
        }

        [SlashCommand("DeleteBclUser", "Delete Bcl User", false)]
        public async Task DeleteBclUser(InteractionContext context,
            [Option("User", "user")] DiscordUser discordUser)
        {
            if (!DiscordConfig.IsTestBot)
            {
                await context.CreateResponseAsync("Unavailable in production.");
                return;
            }

            User? user = userRepository.GetByDiscordId(discordUser.Id);
            if (user is null) return;

            userRepository.Delete(user.Id);
            await userRepository.SaveChangesAsync();

            await context.CreateResponseAsync($"Deleted bcl user for {discordUser.Mention}");
            await discordEngine.Guild.Members[discordUser.Id].RevokeRoleAsync(discordEngine.Guild.Roles[DiscordConfig.Roles.MemberId]);
        }
    }
}
