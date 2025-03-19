using System.Diagnostics;

using BCL.Common.Extensions;
using BCL.Core;
using BCL.Core.Services;
using BCL.Discord.OptionProviders;
using BCL.Domain;
using BCL.Domain.Entities.Analytics;
using BCL.Domain.Entities.Matches;
using BCL.Domain.Entities.Queue;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;

using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

using Humanizer;

#pragma warning disable CS4014

namespace BCL.Discord.Commands.SlashCommands.Admin;
public partial class AdminCommands
{
    public partial class Dev
    {
        [SlashCommand("Migrate", "DON'T")]
        public async Task Migrate(InteractionContext context,
            [Option("Force", "Only works on test")] bool force = false
            //,
            //[Option("TestName", "Only works on test")] string? testName = null
            )
        {
            await context.CreateResponseAsync("Not finished.");
            return;

#pragma warning disable CS0162 // Unreachable code detected
            // ReSharper disable HeuristicUnreachableCode
            await context.DeferAsync();

            #region Validation

            MigrationInfo migrationInfo = analytics.GetMigrationInfo()!;
            if (migrationInfo.Version == CoreConfig.Version && !(force && DiscordConfig.IsTestBot))
            {
                await context.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent($"Migration aborted. Data is already migrated to `{CoreConfig.Version}`."));
                return;
            }

            #endregion

            DateTime started = DateTime.UtcNow;
            await context.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"Current Db Version is `{migrationInfo.Version}`. Attempting to migrate to `{CoreConfig.Version}`. Started: {started.DiscordTime(DiscordTimeFlag.R)}"));
            DiscordMessage feedback = await context.Channel.SendMessageAsync("Migrating...");

            #region Cleanup

            // ReSharper disable UnusedVariable
            List<Match> matches = matchRepository.GetAll().ToList();
            List<Champion> champions = championRepository.GetAll().ToList();
            List<User> users = userRepository.GetAll().ToList();
            // ReSharper restore UnusedVariable

            ProgressBar progress = new(75,
            [
                (100, "Removing invalid entities..."),
                (100, "Registering seasons..."),
            ]);

            string completedTasks = "__**Completed**__";
            await feedback.ModifyAsync($"{completedTasks}\n\n{progress}");
            completedTasks = await RemoveInvalidEntities(matches, users, migrationInfo, completedTasks);
            matches = matchRepository.GetAll().Where(m => m.Outcome is not MatchOutcome.Canceled).ToList();
            users = userRepository.GetAll().ToList();
            progress.Add(100);

            await feedback.ModifyAsync($"{completedTasks}\n\n{progress}");
            RegisterSeasons(matches, migrationInfo, ref completedTasks);
            progress.Add(100);

            #endregion

            // ReSharper disable once EmptyRegion

            migrationInfo.Version = CoreConfig.Version;
            await analytics.SaveChangesAsync();

            await feedback.DeleteAsync();
            string content = $"""
                           Successfully migrated to `{CoreConfig.Version}`.
                           Duration: {(DateTime.UtcNow - started).Humanize(precision: 4)}

                           {completedTasks}
                           """;
            await context.Channel.SendMessageAsync(content);
            discordEngine.Log(content);
            // ReSharper restore HeuristicUnreachableCode
#pragma warning restore CS0162 // Unreachable code detected

        }

        //        // ReSharper disable once UnusedMember.Local
        //#pragma warning disable IDE0051
        //        private async Task<string> GenerateMissingSnapshots_0_6_4(
        //#pragma warning restore IDE0051
        //            DiscordMessage feedback,
        //            ProgressBar progress,
        //            List<Champion> champions,
        //            List<User> userList,
        //            MigrationInfo migrationInfo,
        //            List<Match> matches,
        //            string completedTasks,
        //            string? testName = null)
        //        {
        //            string missingSeason = "Alpha";

        //            #region Cleanup (Delete old data if re-generating)

        //            //Delete old snapshots and stats because they r broken
        //            progress.AddTask(100, "Removing old champion stats...");
        //            await feedback.ModifyAsync($"{completedTasks}\n\n{progress}");
        //            foreach (ChampionStats? stat in champions.SelectMany(champion => champion.Stats))
        //                championRepository.Delete(stat);
        //            await championRepository.SaveChangesAsync();
        //            champions = championRepository.GetAll().ToList();
        //            completedTasks += "\n> - Removed old champion stats.";
        //            progress.Add(100);

        //            //Delete old Alpha data from users
        //            progress.AddTask(100, $"Removing old user stats for `{missingSeason}` season...");
        //            await feedback.ModifyAsync($"{completedTasks}\n\n{progress}");
        //            List<Stats?> oldAlphaStats = userList.Select(u =>
        //            {
        //                Stats? stats = u.GetStats(missingSeason);
        //                if (stats is not null) u.SeasonStats.Remove(stats);
        //                return stats;
        //            }).Where(oldAlphaStats => oldAlphaStats is not null).ToList();
        //            foreach (Stats? stats in oldAlphaStats)
        //                userRepository.Delete(stats!);
        //            await userRepository.SaveChangesAsync();
        //            userList = userRepository.GetAll().ToList();
        //            completedTasks += $"\n> - Removed old user `{missingSeason}` season stats. ({oldAlphaStats.Count} Entities)";
        //            progress.Add(100);

        //            #endregion

        //            #region Debug

        //            string debugStats = string.Empty;
        //            string debugSnapshots = string.Empty;

        //            #endregion

        //            int index = 0;
        //            int lastUpdateIndex = 0;
        //            int alphaIndex = 0;
        //            int missingSeasonMatchCount = 0;
        //            DateTime updated = DateTime.UtcNow;
        //            Dictionary<Ulid, User> users = userList.ToDictionary(u => u.Id);

        //            List<MigrationInfo._Season> seasons = [.. migrationInfo.Seasons.OrderBy(s => s.StartDate)];

        //            #region Mock

        //            if (!string.IsNullOrWhiteSpace(testName) && DiscordConfig.IsTestBot)
        //            {
        //                missingSeason = $"Test_{testName}";
        //                List<MigrationInfo._Season> seasonsMock =
        //                [
        //                    new(missingSeason, DateTime.UtcNow, 2)
        //                ];
        //                List<Match> matchesMock = matches.Where(m =>
        //                        m.Outcome is not MatchOutcome.Canceled &&
        //                        m.Season == "Alpha")
        //                    .OrderBy(m => m.RecordedAt)
        //                    .Take(100)
        //                    .ToList();

        //                seasons = seasonsMock;
        //                matches = matchesMock;
        //            }

        //            #endregion

        //            progress.AddTask(matches.Count, "Generating missing snapshot data...");
        //            foreach (MigrationInfo._Season? season in seasons)
        //            {
        //                #region Setup Season

        //                List<Match> seasonMatches = [.. matches.Where(m => m.Season == season.Label || !string.IsNullOrWhiteSpace(testName)) //if testing use whatever mock matches we provide
        //                    .OrderBy(m => m.RecordedAt)];

        //                int seasonMatchCount = 0;
        //                // ReSharper disable InconsistentNaming
        //                int seasonMatchCount_Pro = 0;
        //                int seasonMatchCount_Standard = 0;
        //                int seasonMatchCount_Tournament = 0;
        //                int seasonMatchCount_Event = 0;
        //                int seasonMatchCount_Custom = 0;
        //                // ReSharper restore InconsistentNaming

        //                #endregion

        //                foreach (Match? match in seasonMatches)
        //                {
        //                    (updated, lastUpdateIndex) = await UpdateFeedbackMessage(feedback, progress, completedTasks, index, updated, lastUpdateIndex, $"**{index}** / **{matches.Count}** matches. `{season.Label}`");

        //                    #region Setup Match

        //                    Dictionary<Ulid, User?> players = match.PlayerInfos.Select(p =>
        //                        {
        //                            users.TryGetValue(p.Id, out User? u);
        //                            return u;
        //                        })
        //                        .Where(u => u != null)
        //                        .ToDictionary(p => p!.Id);

        //                    List<Champion> matchChampions = match.Draft.Steps
        //                        .Select(s => new[] { s.TokenId1, s.TokenId2 })
        //                        .SelectMany(x => x)
        //                        .Distinct()
        //                        .Where(id => id != Ulid.Empty && id != default)
        //                        .Select(id => champions.Single(c => c.Id == id))
        //                        .ToList();

        //                    index++;
        //                    seasonMatchCount++;
        //                    switch (match.League)
        //                    {
        //                        case League.Pro: seasonMatchCount_Pro++; break;
        //                        case League.Standard: seasonMatchCount_Standard++; break;
        //                        case League.Event: seasonMatchCount_Event++; break;
        //                        case League.Tournament: seasonMatchCount_Tournament++; break;
        //                        case League.Custom: seasonMatchCount_Custom++; break;
        //                        default: throw new UnreachableException();
        //                    }

        //                    #endregion

        //                    #region Generate Champion Data

        //                    if (matchChampions.Count != 0) //Alpha was using string id's so UNLUCKY it's all lost
        //                    {
        //                        foreach (Champion? champion in matchChampions)
        //                        {
        //                            ChampionStats? stats = champion.Stats.SingleOrDefault(s => s.Season == season.Label);
        //                            if (stats == null)
        //                            {
        //                                stats = new ChampionStats
        //                                {
        //                                    Season = season.Label,
        //                                    DateOverride = match.CreatedAt,
        //                                    Snapshots =
        //                                    [
        //                                        new()
        //                                        {
        //                                            Season = season.Label,
        //                                            DateOverride = match.CreatedAt
        //                                        }
        //                                    ]
        //                                };
        //                                champion.Stats.Add(stats);
        //                            }

        //                            ChampionStatsSnapshot newSnapshot = new(stats.LatestSnapshot)
        //                            {
        //                                DateOverride = match.CreatedAt
        //                            };
        //                            StatsService.SetSnapshotValues(champion, match, newSnapshot);

        //                            newSnapshot.MatchCount = seasonMatchCount;
        //                            int matchCountSpecific = match.League switch
        //                            {
        //                                League.Pro => seasonMatchCount_Pro,
        //                                League.Standard => seasonMatchCount_Standard,
        //                                League.Event => seasonMatchCount_Event,
        //                                League.Tournament => seasonMatchCount_Tournament,
        //                                League.Custom => seasonMatchCount_Custom,
        //                                _ => throw new UnreachableException()
        //                            };

        //                            newSnapshot.GetType().GetProperty($"MatchCount_{match.League}")?
        //                                .GetSetMethod()?.Invoke(newSnapshot, [matchCountSpecific]);

        //                            stats.Snapshots.Add(newSnapshot);
        //                        }
        //                    }

        //                    #endregion

        //                    bool generateNew = season.Label.Equals(missingSeason, StringComparison.CurrentCultureIgnoreCase);

        //                    #region Generate Missing Data
        //                    if (!generateNew)
        //                    {
        //                        foreach (User player in players.Values)
        //                        {
        //                            Stats? stats = player.GetStats(season.Label);
        //                            if (stats is null)
        //                            {
        //                                try
        //                                {
        //                                    debugStats += $"""

        //                                                    NULL_STATS | {player.InGameName} | {player.DiscordId} | {season.Label} | {match.League}{player.SeasonStats.Aggregate("", (current, stat) => $"{current}\n    {stat.Season} | Played {stat.LatestSnapshot!.GamesPlayed} | Snapshots {stat.Snapshots.Count}")}\n
        //                                                    """;
        //                                }
        //                                catch (Exception e)
        //                                {
        //                                    debugStats += $"\n> {e.Message}";
        //                                }
        //                                continue;
        //                            }

        //                            if (match.League is League.Pro) stats.Membership = League.Pro; //if they played a pro game they were pro that season for simplicity
        //                            if (!match.IsCaptain(player)) continue;

        //                            StatsSnapshot? current = stats.Snapshots.MinBy(s =>
        //                                Math.Abs((s.RecordedAt - match.RecordedAt).TotalMinutes));
        //                            if (current is null)
        //                            {
        //                                try
        //                                {
        //                                    debugSnapshots += $"""

        //                                                        NULL_SNAPSHOT | {player.InGameName} | {player.DiscordId} | {season.Label} | Snapshots: {stats.Snapshots.Count}
        //                                                        """;
        //                                }
        //                                catch (Exception e)
        //                                {
        //                                    debugSnapshots += $"\n> {e.Message}";
        //                                }
        //                                continue;
        //                            }

        //                            int captainGames = stats.Snapshots.Max(s => s.CaptainGames);
        //                            int captainWins = stats.Snapshots.Max(s => s.CaptainWins);
        //                            foreach (StatsSnapshot? snap in stats.Snapshots.Where(s => s.RecordedAt < current.RecordedAt))
        //                            {
        //                                //Only set missing data (0 value)
        //                                if (snap.CaptainGames == 0) snap.CaptainGames = captainGames;
        //                                if (snap.CaptainWins == 0) snap.CaptainWins = captainWins;
        //                            }

        //                            current.CaptainGames = captainGames + 1;
        //                            if (match.Won(player)) current.CaptainWins = captainWins + 1;
        //                        }
        //                    }
        //                    #endregion
        //                    #region Else Generate Alpha Snapshots
        //                    else
        //                    {
        //                        missingSeasonMatchCount++;

        //                        double team1Elo = match.League == League.Pro
        //                            ? match.Team1_PlayerInfos.Average(p =>
        //                                players.TryGetValue(p.Id, out User? u)
        //                                    ? u.GetStats(season.Label)?.LatestSnapshot?.Rating ?? DomainConfig.DefaultRating
        //                                    : DomainConfig.DefaultRating)
        //                            : match.Team1_PlayerInfos.Average(p =>
        //                                players.TryGetValue(p.Id, out User? u)
        //                                    ? u.GetStats(season.Label)?.LatestSnapshot?.Rating_Standard ?? DomainConfig.DefaultRating
        //                                    : DomainConfig.DefaultRating);

        //                        double team2Elo = match.League == League.Pro
        //                            ? match.Team2_PlayerInfos.Average(p =>
        //                                players.TryGetValue(p.Id, out User? u)
        //                                    ? u.GetStats(season.Label)?.LatestSnapshot?.Rating ?? DomainConfig.DefaultRating
        //                                    : DomainConfig.DefaultRating)
        //                            : match.Team2_PlayerInfos.Average(p =>
        //                                players.TryGetValue(p.Id, out User? u)
        //                                    ? u.GetStats(season.Label)?.LatestSnapshot?.Rating_Standard ?? DomainConfig.DefaultRating
        //                                    : DomainConfig.DefaultRating);

        //                        double eloK = match.League switch
        //                        {
        //                            League.Pro => CoreConfig.Queue.RatingShift_Pro,
        //                            League.Standard => CoreConfig.Queue.RatingShift_Standard,
        //                            _ => 0
        //                        };

        //                        //Alpha matches didn't save the rating change so we approximate it using our current system
        //                        double eloShift = RankingService.GetEloShift(eloK, team1Elo, team2Elo, match.Outcome);

        //                        foreach (User player in players.Values)
        //                        {
        //                            Stats? stats = player.GetStats(season.Label);
        //                            if (stats == null)
        //                            {
        //                                stats = new Stats
        //                                {
        //                                    Season = season.Label,
        //                                    Membership = League.Standard,
        //                                    DateOverride = match.CreatedAt,
        //                                    Disclaimer = $"> `{season.Label}` stats were generated using `{DomainConfig.Season}` systems, they might __not__ be *100% accurate*.",
        //                                    Snapshots =
        //                                    [
        //                                        new()
        //                                        {
        //                                            Season = season.Label,
        //                                            Rating = DomainConfig.DefaultRating,
        //                                            Rating_Standard = DomainConfig.DefaultRating,
        //                                            DateOverride = match.CreatedAt.AddSeconds(-1)
        //                                        }
        //                                    ]
        //                                };
        //                                player.SeasonStats.Add(stats);
        //                            }

        //                            if (match.League is League.Pro) stats.Membership = League.Pro;

        //                            StatsSnapshot newSnapshot = new(stats.LatestSnapshot)
        //                            {
        //                                League = match.League,
        //                                DateOverride = match.CreatedAt
        //                            };

        //                            Match.Side side = match.GetSide(player);
        //                            switch (match.League)
        //                            {
        //                                case League.Standard when side is Match.Side.Team1:
        //                                    newSnapshot.Rating_Standard += eloShift;
        //                                    break;
        //                                case League.Standard when side is Match.Side.Team2:
        //                                    newSnapshot.Rating_Standard -= eloShift;
        //                                    break;

        //                                case League.Pro when side is Match.Side.Team1:
        //                                    newSnapshot.Rating += eloShift;
        //                                    newSnapshot.Rating_Standard += eloShift;
        //                                    break;
        //                                case League.Pro when side is Match.Side.Team2:
        //                                    newSnapshot.Rating -= eloShift;
        //                                    newSnapshot.Rating_Standard -= eloShift;
        //                                    break;

        //                                case League.Event:
        //                                case League.Tournament:
        //                                case League.Custom: break;
        //                                default: throw new UnreachableException();
        //                            }

        //                            newSnapshot.GamesPlayed++;
        //                            if (match.IsCaptain(player)) newSnapshot.CaptainGames++;

        //                            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        //                            switch (match.League)
        //                            {
        //                                case League.Pro:
        //                                    newSnapshot.GamesPlayed_Pro++;
        //                                    break;
        //                                case League.Standard:
        //                                    newSnapshot.GamesPlayed_Standard++;
        //                                    break;
        //                                case League.Tournament:
        //                                    newSnapshot.GamesPlayed_Tournament++;
        //                                    break;
        //                            }

        //                            if (match.Won(player))
        //                            {
        //                                newSnapshot.GamesWon++;
        //                                if (match.IsCaptain(player)) newSnapshot.CaptainWins++;

        //                                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        //                                switch (match.League)
        //                                {
        //                                    case League.Pro:
        //                                        newSnapshot.GamesWon_Pro++;
        //                                        break;
        //                                    case League.Standard:
        //                                        newSnapshot.GamesWon_Standard++;
        //                                        break;
        //                                    case League.Tournament:
        //                                        newSnapshot.GamesWon_Tournament++;
        //                                        break;
        //                                }
        //                            }

        //                            stats.Snapshots.Add(newSnapshot);
        //                            alphaIndex++;
        //                        }
        //                    }
        //                    #endregion
        //                }
        //                Console.WriteLine($"{season.Label}: {seasonMatches.Count} | {seasonMatchCount}");
        //            }

        //            #region Captain Data - Trailing Snapshots (One time use)

        //            index = 0;
        //            lastUpdateIndex = 0;
        //            progress.AddTask(users.Count, "Generating missing captain data...");
        //            foreach (User? user in users.Values)
        //            {
        //                (updated, lastUpdateIndex) = await UpdateFeedbackMessage(feedback, progress, completedTasks, index, updated, lastUpdateIndex, $"**{index}** / **{users.Count}** users. `{user.InGameName}`");

        //                foreach (Stats stat in user.SeasonStats)
        //                {
        //                    int captainGames = stat.Snapshots.Max(s => s.CaptainGames);
        //                    int captainWins = stat.Snapshots.Max(s => s.CaptainWins);
        //                    StatsSnapshot? after = stat.Snapshots.OrderBy(s => s.RecordedAt)
        //                        .FirstOrDefault(s => s.CaptainGames != 0);
        //                    if (after is null) continue;

        //                    foreach (StatsSnapshot? snapshot in stat.Snapshots.Where(s => (s.RecordedAt >= after.RecordedAt &&
        //                                 s.CaptainGames == 0) || s.CaptainWins == 0))
        //                    {
        //                        snapshot.CaptainGames = captainGames;
        //                        snapshot.CaptainWins = captainWins;
        //                    }
        //                }

        //                index++;
        //            }

        //            #endregion

        //            #region Tests

        //            User? dev = users.Values.SingleOrDefault(u => u.DiscordId == DiscordConfig.DevId);
        //            Stats? devStats = dev?.GetStats(missingSeason);
        //            completedTasks += $"\n> - Test: **{devStats?.Snapshots.Count}**/**{devStats?.LatestSnapshot?.GamesPlayed ?? 0 + 1}** `{missingSeason}` snapshots for {dev?.Mention}. {(devStats?.Snapshots.Count == (devStats?.LatestSnapshot?.GamesPlayed ?? 0) + 1 ? "`Passed`:+1:" : "`Failed`⚠️")}";

        //            User randomUser = users.Values.ElementAt(Random.Shared.Next(users.Count - 1));
        //            Stats randomUserStats = randomUser.SeasonStats.ElementAt(Random.Shared.Next(randomUser.SeasonStats.Count - 1));
        //            completedTasks += $"\n> - Test: **{randomUserStats.Snapshots.Count}**/**{randomUserStats.LatestSnapshot?.GamesPlayed ?? 0 + 1}** `{randomUserStats.Season}` snapshots for {randomUser.Mention}. {(randomUserStats.Snapshots.Count == (randomUserStats.LatestSnapshot?.GamesPlayed ?? 0) + 1 ? "`Passed`:+1:" : "`Failed`⚠️")}";

        //            #endregion

        //            completedTasks += $"\n> - Simulated **{matches.Count}** matches to generate missing snapshots.";
        //            completedTasks += $"\n> - Generated **{champions.SelectMany(c => c.Stats.SelectMany(s => s.Snapshots)).Count()}** snapshots for champion stats.";
        //            completedTasks += $"\n> - Generated **{alphaIndex}** snapshots for users for `{missingSeason}` season (**{missingSeasonMatchCount}**/**{matches.Count(m => m.Season == missingSeason && m.Outcome is not MatchOutcome.Canceled)}** matches).";

        //            #region Debug

        //            await using MemoryStream debugStream = new();
        //            await using StreamWriter writer = new(debugStream);
        //            string debugContent = $"""

        //                                {debugStats}
        //                                {debugSnapshots}

        //                                """;
        //            await writer.WriteAsync(debugContent);
        //            await writer.FlushAsync();

        //            debugStream.Position = 0;
        //            await feedback.Channel.SendMessageAsync(new DiscordMessageBuilder()
        //                .WithContent("Debug info.")
        //                .AddFile($"DebugInfo_Migration_{DateTime.UtcNow:yyyyMMddhhmm}.txt", debugStream));

        //            #endregion

        //            return completedTasks;
        //        }

        //private static Task<(DateTime updated, int lastUpdateIndex)> UpdateFeedbackMessage(
        //    DiscordMessage feedback,
        //    ProgressBar progress,
        //    string completedTasks,
        //    int index,
        //    DateTime updated,
        //    int lastUpdateIndex,
        //    string info = "")
        //{
        //    if ((DateTime.UtcNow - updated) <= TimeSpan.FromSeconds(5)) return Task.FromResult((updated, lastUpdateIndex));

        //    progress.Add(index - lastUpdateIndex);
        //    updated = DateTime.UtcNow;
        //    lastUpdateIndex = index;

        //    feedback.ModifyAsync($"{completedTasks}\n\n{progress}\n{info}");

        //    return Task.FromResult((updated, lastUpdateIndex));
        //}

        private async Task<string> RemoveInvalidEntities(
            IEnumerable<Match> matches,
            IReadOnlyCollection<User> users,
            MigrationInfo migrationInfo,
            string completedTasks)
        {
            //Matches
            List<Match> brokenMatches = matches.Where(m =>
                string.IsNullOrWhiteSpace(m.Season) ||
                m.Season.Equals("Deleted") || m.Season.Contains("Test") ||
                m.Outcome == MatchOutcome.InProgress).ToList();
            foreach (Match? match in brokenMatches) matchRepository.Delete(match.Id);
            if (brokenMatches.Count > 0) completedTasks += $"\n> - Deleted **{brokenMatches.Count}** invalid matches.";

            //Stats
            List<Stats> brokenStats = users.SelectMany(u =>
            {
                List<Stats> stats = u.SeasonStats.Where(stats =>
                        migrationInfo.Seasons.All(s => stats.Season != s.Label) ||
                        (stats.Season != DomainConfig.Season && stats.Snapshots.Count is 0 or 1))
                    .ToList();

                u.SeasonStats.RemoveAll(stat => stats.Any(broken => broken.Id == stat.Id));

                return stats;
            }).ToList();
            foreach (Stats? stat in brokenStats) userRepository.Delete(stat);
            if (brokenStats.Count > 0) completedTasks += $"\n> - Deleted **{brokenStats.Count}** invalid stats.";

            //Snapshots (omitting the ones deleted earlier as deleting stats also removes associated snapshots)
            List<StatsSnapshot> brokenSnapshots = users.SelectMany(u =>
            {
                return u.SeasonStats.SelectMany(stats =>
                {
                    List<StatsSnapshot> snapshots = stats.Snapshots.Where(snapshot =>
                            migrationInfo.Seasons.All(s => snapshot.Season != s.Label) &&
                            brokenStats.All(bs => bs.Snapshots.All(snap => snap.Id != bs.Id)))
                        .ToList();

                    stats.Snapshots.RemoveAll(s => snapshots.Any(b => b.Id == s.Id));

                    return snapshots;
                });
            }).ToList();
            foreach (StatsSnapshot? snapshot in brokenSnapshots) userRepository.Delete(snapshot);
            if (brokenSnapshots.Count > 0) completedTasks += $"\n> -Deleted **{brokenSnapshots.Count}** invalid stats.";

            await matchRepository.SaveChangesAsync();

            return completedTasks;
        }

        internal static void RegisterSeasons(IEnumerable<Match> matches, MigrationInfo migrationInfo, ref string tasks)
        {
            List<MigrationInfo._Season> seasons = matches.GroupBy(s => s.Season)
                .Where(g => g.First().Season is not null)
                .Select(g => new MigrationInfo._Season(
                        g.First().Season!,
                        g.First().CreatedAt,
                        g.Count(m => m.Outcome is not MatchOutcome.Canceled)))
                .ToList();

            if (seasons.All(s => !s.Label.Equals(DomainConfig.Season, StringComparison.CurrentCultureIgnoreCase)))
                seasons.Add(new MigrationInfo._Season(DomainConfig.Season, DateTime.UtcNow, 0));

            foreach (MigrationInfo._Season? season in seasons)
                migrationInfo.AddSeason(season);

            SeasonChoiceProvider.Initialize(migrationInfo.Seasons);
            tasks += $"\n> - Registered **{migrationInfo.Seasons.Length}** seasons.";
        }

        //Let's do some 4fun stuff
        internal class ProgressBar
        {
            private class ProgressBarTask
            {
                public int StartIndex { get; init; }
                public string Label { get; init; } = "Missing Task Information";
            }

            public int Capacity { get; set; }
            public int CurrentValue { get; set; }

            public int DisplayBlocks { get; set; }
            public int FilledBlocks => (int)Math.Ceiling((double)CurrentValue / Capacity * DisplayBlocks);

            private const string Empty = " ";
            private const string Completed = "■";

            private readonly List<ProgressBarTask> _tasks = [];

            public ProgressBar(int displayBlocks, IEnumerable<(int, string)> tasks)
            {
                DisplayBlocks = displayBlocks;
                foreach ((int weight, string task) in tasks) AddTask(weight, task);
            }

            public void AddTask(int weight, string task)
            {
                _tasks.Add(new ProgressBarTask { StartIndex = Capacity, Label = task });
                Capacity += weight;
            }

            /// <summary>
            /// Progress the progress bar :^)
            /// </summary>
            /// <param name="value"></param>
            public void Add(int value) => CurrentValue += value;
            public override string ToString() =>
                $"""
                 *{_tasks.Last(t => t.StartIndex <= CurrentValue).Label}*
                 `[{string.Concat(Enumerable.Repeat(Completed, FilledBlocks))}{string.Concat(Enumerable.Repeat(Empty, DisplayBlocks - FilledBlocks))}]`
                 """;
        }
    }
}
