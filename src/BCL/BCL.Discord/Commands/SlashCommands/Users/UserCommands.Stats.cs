using System.Globalization;

using BCL.Common.Extensions;
using BCL.Core;
using BCL.Discord.Bot;
using BCL.Discord.OptionProviders;
using BCL.Domain;
using BCL.Domain.Entities.Analytics;
using BCL.Domain.Entities.Matches;
using BCL.Domain.Entities.Queue;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

using Humanizer;
using Humanizer.Localisation;

using Microsoft.Extensions.DependencyInjection;

namespace BCL.Discord.Commands.SlashCommands.Users;
public partial class UserCommands
{
    [SlashCommandGroup("Stats", "Get stats for stuff!")]
    [SlashModuleLifespan(SlashModuleLifespan.Scoped)]
    public partial class Stats(
        IUserRepository userRepository,
        IMatchRepository matchRepository,
        IMapRepository mapRepository,
        IChampionRepository championRepository,
        IAnalyticsRepository analytics,
        DiscordEngine discordEngine) : ApplicationCommandModule
    {
        [SlashCommand("Champions", "Displays stats for Champions")]
        public async Task Champions(InteractionContext context)
        {
            await context.CreateResponseAsync("Getting stats for Champions...");
            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(GetChampionStatsEmbed(
                    GetGenericEmbed(context.Guild.IconUrl),
                    [DomainConfig.Season],
                    context.Services))
                .AddComponents(BuildFilterSelectComponent(
                    null, context.User.Id, context.Guild.Emojis, [DomainConfig.Season],
                    SelectOption.GetOptions(analytics.GetMigrationInfo()!, true),
                    SelectFor.ChampionStats)));
        }

        [SlashCommand("Maps", "Displays stats for Maps")]
        public async Task Maps(InteractionContext context)
        {
            await context.CreateResponseAsync("Getting stats for Maps...");
            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(GetMapStatsEmbed(GetGenericEmbed(context.Guild.IconUrl))));
        }

        [SlashCommand("Regions", "Displays stats for Regions")]
        public async Task Regions(InteractionContext context)
        {
            await context.CreateResponseAsync("Getting stats for Regions...");
            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(GetRegionStatsEmbed(
                    GetGenericEmbed(context.Guild.IconUrl),
                    [DomainConfig.Season],
                    context.Services
                ))
                .AddComponents(BuildFilterSelectComponent(
                    null, context.User.Id, context.Guild.Emojis, [DomainConfig.Season],
                    SelectOption.GetOptions(analytics.GetMigrationInfo()!),
                    SelectFor.RegionStats)));
        }

        [SlashCommand("LastQueued", "Find out when the user last queued")]
        public async Task LastQueued(InteractionContext context,
            [Option("DiscordMember", "Discord Member")] DiscordUser? discordUser = null)
        {
            discordUser ??= context.User;
            User? user = userRepository.GetByDiscordId(discordUser.Id);
            string? lastQueued = user?.LastQueued is null
                ? "Not even trying. :thumbs_down:"
                : user.LastQueued?.DiscordTime(DiscordTimeFlag.R);
            string? lastPlayed = user?.LastPlayed is null
                ? "Not gaming. :zzz:"
                : user.LastPlayed?.DiscordTime(DiscordTimeFlag.R);

            await context.CreateResponseAsync($"{discordUser.Mention} Last Queued: {lastQueued} | Last Played: {lastPlayed}");
        }

        [SlashCommand("ChampionActionsSince", "How many times has the champ been picked or banned.")]
        public async Task ChampionActionsSince(InteractionContext context,
            [Autocomplete(typeof(ChampionAutocompleteProvider))]
            [Option("Champion", "Champion")] string championId,
            [Option("DraftAction", "Action")] DraftAction action,
            [Option("Date", "Date formatted as Day/Month/Year")] string dateInput)
        {
            if (!DateTime.TryParseExact(dateInput, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                await context.CreateResponseAsync($"Invalid datetime format. Provided `{dateInput}`. Please use `Day/Month/Year` format. Example `27/06/2022`");
                return;
            }

            Champion? champion = await championRepository.GetByIdAsync(championId);
            await context.CreateResponseAsync($"Attempting to get `{action}` info for __{champion!.Name}__ since {date.DiscordTime(DiscordTimeFlag.D)}.");
            List<DraftStep> steps = matchRepository.Get(m =>
                    m.Outcome != MatchOutcome.Canceled &&
                    m.CreatedAt > date)
                .SelectMany(m => m.Draft.Steps)
                .Where(s => s.Action == action)
                .ToList();

            int count = steps.Count(s => s.TokenId1 == champion.Id) + steps.Count(s => s.TokenId2 == champion.Id);

            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"**{champion.Name}** - {action} - {count} times since {date.DiscordTime(DiscordTimeFlag.D)}"));
        }

        internal static DiscordEmbedBuilder GetGenericEmbed(string iconUrl) =>
            new DiscordEmbedBuilder()
                .WithAuthor(DomainConfig.ServerName, null, iconUrl)
                .WithTimestamp(DateTime.UtcNow)
                .WithColor(DiscordColor.Aquamarine)
                .WithFooter($"Version {CoreConfig.Version}", iconUrl);

        internal static DiscordEmbedBuilder GetChampionStatsEmbed(
            DiscordEmbedBuilder embed,
            string[] seasons,
            IServiceProvider services)
        {
            embed.WithTitle($"Champions - {string.Join('|', seasons)}")
                .AddField("Values", """
                                    ```
                                    Champion  | WinRate       | PickRate      | BanRate       ```
                                    """);
            using IServiceScope scope = services.CreateScope();

            const double goodThreshold = 0.60;
            const double badThreshold = 0.47;

            List<Champion> champions = scope.ServiceProvider.GetService<IChampionRepository>()!.GetAll()
                .Where(c => c.Stats.Any(s => seasons.Any(ss => ss == s.Season))).ToList();

            if (champions.Count == 0)
            {
                embed.AddField("No data", $"There is no data for `{string.Join('|', seasons)}`");
                return embed;
            }

            List<(double, string)> decent = [.. champions.Select(ChampionInfo).OrderByDescending(s => s.Item1)];
            List<(double, string)> op = decent.Where(s => s.Item1 > goodThreshold).ToList();
            List<(double, string)> dog = decent.Where(s => s.Item1 < badThreshold).ToList();
            decent.RemoveAll(c => op.Any(o => c == o));
            decent.RemoveAll(c => dog.Any(d => c == d));

            //Good
            string high = $"""
                        ```diff
                        {op.Aggregate("", (current, champion) => $"{current}\n{champion.Item2}").Trim()}
                        ```
                        """;
            if (high.Length > 1024)
            {
                int middle = op.Count / 2;
                string page1 = $"""
                             ```diff
                             {op.Take(middle).Aggregate("", (current, champion) => $"{current}\n{champion.Item2}").Trim()}
                             ```
                             """;
                string page2 = $"""
                             ```diff
                             {op.Skip(middle).Aggregate("", (current, champion) => $"{current}\n{champion.Item2}").Trim()}
                             ```
                             """;
                embed.AddField("OP", page1)
                    .AddField("OP", page2);
            }
            else
            {
                embed.AddField("OP", high);
            }

            //Decent
            string other = $"""
                         ```diff
                         {decent.Aggregate("", (current, champion) => $"{current}\n{champion.Item2}").Trim()}
                         ```
                         """;
            if (other.Length > 1024)
            {
                int middle = decent.Count / 2;
                string page1 = $"""
                             ```diff
                             {decent.Take(middle).Aggregate("", (current, champion) => $"{current}\n{champion.Item2}").Trim()}
                             ```
                             """;
                string page2 = $"""
                             ```diff
                             {decent.Skip(middle).Aggregate("", (current, champion) => $"{current}\n{champion.Item2}").Trim()}
                             ```
                             """;
                embed.AddField("Decent", page1)
                    .AddField("Decent", page2);
            }
            else
            {
                embed.AddField("Decent", other);
            }

            //Bad
            string low = $"""
                       ```diff
                       {dog.Aggregate("", (current, champion) => $"{current}\n{champion.Item2}").Trim()}
                       ```
                       """;
            if (low.Length > 1024)
            {
                int middle = dog.Count / 2;
                string page1 = $"""
                             ```diff
                             {dog.Take(middle).Aggregate("", (current, champion) => $"{current}\n{champion.Item2}").Trim()}
                             ```
                             """;
                string page2 = $"""
                             ```diff
                             {dog.Skip(middle).Aggregate("", (current, champion) => $"{current}\n{champion.Item2}").Trim()}
                             ```
                             """;
                embed.AddField("Dog", page1)
                    .AddField("Dog", page2);
            }
            else
            {
                embed.AddField("Dog", low);
            }

            return embed;

            (double, string) ChampionInfo(Champion champion)
            {
                List<ChampionStats> stats = champion.Stats.Where(s => seasons.Any(ss => ss == s.Season)).ToList();

                int picked = stats.Sum(s => s.LatestSnapshot?.GamesPlayed ?? 0);
                int won = stats.Sum(s => s.LatestSnapshot?.GamesWon ?? 0);
                int banned = stats.Sum(s => s.LatestSnapshot?.Banned ?? 0);
                int matchCount = stats.Sum(s => s.LatestSnapshot?.MatchCount ?? 0);

                string championPicks = $"{picked}{new string(' ', 4 - picked.ToString().Length)}";
                string championWins = $"{won}{new string(' ', 4 - won.ToString().Length)}";
                string championBans = $"{banned}{new string(' ', 4 - banned.ToString().Length)}";

                double winRate = (double)won / picked;
                double pickRate = (double)picked / matchCount;
                double banRate = (double)banned / matchCount;

                string decorator = winRate switch
                {
                    > goodThreshold => "+",
                    < badThreshold => "-",
                    _ => string.Empty
                };

                string championName = $"{decorator}{champion.Name}";
                championName = $"{championName}{new string(' ', 9 - championName.Length)}";

                string win = $"{winRate:0.00%}"; //100.00%
                win = $"{win}{new string(' ', 7 - win.Length)}";
                string pick = $"{pickRate:0.00%}";
                pick = $"{pick}{new string(' ', 7 - pick.Length)}";
                string ban = $"{banRate:0.00%}";
                ban = $"{ban}{new string(' ', 7 - ban.Length)}";

                return (winRate, $"{championName} | {win}  {championWins} | {pick}  {championPicks} | {ban}  {championBans}");
            }
        }
        private DiscordEmbedBuilder GetMapStatsEmbed(DiscordEmbedBuilder embed)
        {
            List<Map> maps = [.. mapRepository.GetAll().OrderByDescending(m => m.Frequency)];
            int longestName = maps.OrderByDescending(m => m.Name.Length).First().Name.Length + 1; //+1 cus decorator
            int totalStandardFrequency = maps.Where(m => !m.Disabled).Sum(m => m.Frequency);
            int totalProFrequency = maps.Where(m => m is { Disabled: false, Pro: true }).Sum(m => m.Frequency);

            List<Map> proMaps = maps.Where(m => m is { Pro: true, Disabled: false }).ToList();
            List<Map> disabledMaps = maps.Where(m => m.Disabled).ToList();
            maps.RemoveAll(m => proMaps.Any(p => p == m) || disabledMaps.Any(d => d == m)); //whatever

            string pro = $"""
                       ```diff
                       {proMaps.Aggregate("", (current, map) => $"{current}\n{MapInfo(map)}").Trim()}
                       ```
                       """;
            string disabled = $"""
                            ```diff
                            {disabledMaps.Aggregate("", (current, map) => $"{current}\n{MapInfo(map)}").Trim()}
                            ```
                            """;
            string other = $"""
                         ```diff
                         {maps.Aggregate("", (current, map) => $"{current}\n{MapInfo(map)}").Trim()}
                         ```
                         """;

            embed.WithTitle("Maps")
                .AddField("Pro and Standard", pro)
                .AddField("Standard only", other)
                .AddField("Disabled", disabled);

            return embed;

            string MapInfo(Map map)
            {
                string decorator = map.Pro switch
                {
                    true when !map.Disabled => "+",
                    _ when map.Disabled => "-",
                    _ => string.Empty
                };

                string mapName = $"{decorator}{map.Name.Split(" - ").First()} {map.Variant}";
                mapName = $"{mapName}{new string(' ', longestName - mapName.Length)}";

                double standardFrequency = map.Disabled ? 0 : (double)map.Frequency / totalStandardFrequency;
                double proFrequency = (map.Disabled || !map.Pro) ? 0 : (double)map.Frequency / totalProFrequency;

                string mapGamesPlayed = $"{map.GamesPlayed}{new string(' ', 4 - map.GamesPlayed.ToString().Length)}";
                string proInfo = map.Pro ? $"P: {proFrequency:00.00%}\n> " : string.Empty;
                string mapInfo = map.Disabled
                    ? $"{mapName} Played: {mapGamesPlayed}"
                    : $"{mapName} Played: {mapGamesPlayed} | {proInfo}S: {standardFrequency:00.00%}";
                return mapInfo;
            }
        }

        internal static DiscordEmbedBuilder GetRegionStatsEmbed(
            DiscordEmbedBuilder embed,
            string[] seasons,
            IServiceProvider services)
        {
            embed.WithTitle($"Regions - {string.Join('|', seasons)}");

            using IServiceScope scope = services.CreateScope();

            List<Match> matches = scope.ServiceProvider.GetService<IMatchRepository>()!.Get(m =>
                    seasons.Any(s => m.Season == s) &&
                    m.Outcome != MatchOutcome.Canceled &&
                    m.Outcome != MatchOutcome.InProgress)
                .ToList();

            List<Match> euMatches = matches.Where(m => m.Region is Region.Eu).ToList();
            List<Match> naMatches = matches.Where(m => m.Region is Region.Na).ToList();

            List<User> users = scope.ServiceProvider.GetService<IUserRepository>()!
                .Get(u => u.SeasonStats.Any(s => seasons.Any(ss => ss == s.Season))).ToList();

            List<User> euUsers = users.Where(u => u.Server is Region.Eu).ToList();
            List<User> naUsers = users.Where(u => u.Server is Region.Na).ToList();

            IAnalyticsRepository analytics = scope.ServiceProvider.GetService<IAnalyticsRepository>()!;

            List<RegionStats?> euDraftTimes = seasons.Select(s => analytics.GetRegionStats(Region.Eu, s)).ToList();
            List<RegionStats?> naDraftTimes = seasons.Select(s => analytics.GetRegionStats(Region.Na, s)).ToList();

            AddRegionFields(embed, "Europe", seasons, euMatches, euUsers, euDraftTimes);
            AddRegionFields(embed, "N/A", seasons, naMatches, naUsers, naDraftTimes);

            embed.AddField("Disclaimer", "*To qualify for stats you need to have at least **25** games played in the league and season.*");

            return embed;
        }

        private static void AddRegionFields(DiscordEmbedBuilder embed, string label, string[] seasons,
            IReadOnlyCollection<Match> matches,
            IReadOnlyCollection<User> users,
            IReadOnlyCollection<RegionStats?> regionStats)
        {
            #region Matches

            int pro = matches.Count(m => m.League is League.Pro);
            int standard = matches.Count(m => m.League is League.Standard);
            int other = matches.Count - (standard + pro);
            int today = matches.Count(m => m.CreatedAt.Date == DateTime.Today);
            int yesterday = matches.Count(m => m.CreatedAt.Date == DateTime.Today.AddDays(-1).Date);
            int thisMonth = matches.Count(m => m.CreatedAt.Date.Month == DateTime.Today.Month);
            int thisSeason = matches.Count(m => m.Season == DomainConfig.Season);

            #endregion

            #region Users

            int usersPro = users.Count(u => u.Pro);
            int usersStandard = users.Count - usersPro;
            int active = users.Count(u => (DateTime.UtcNow - u.LastPlayed)?.Days < 7);

            #endregion

            #region Draft

            int timedDrafts = regionStats.Sum(d => d?.TimedDrafts ?? 0);
            TimeSpan average = timedDrafts == 0
                ? TimeSpan.MaxValue
                : new TimeSpan(Convert.ToInt64(
                    regionStats.SelectMany(d => Enumerable.Repeat(d?.Average ?? TimeSpan.Zero, d?.TimedDrafts ?? 0))
                        .Average(s => s.Ticks)));
            RegionStats? longest = regionStats.MaxBy(d => d?.LongestTime ?? TimeSpan.MinValue);
            RegionStats? shortest = regionStats.MinBy(d => d?.ShortestTime ?? TimeSpan.MaxValue);

            #endregion

            #region Peaks

            Peaks peaks = GetPeaks(users, seasons);

            const string noData = "~~No Data~~";

            string highestRatingPro = peaks.HighestRatingPro.Value == 0 ? noData
                : $"`{peaks.HighestRatingPro.Value}` <@{peaks.HighestRatingPro.DiscordId}>";

            string lowestRatingPro = peaks.LowestRatingPro.Value is 0 or double.MaxValue ? noData
                : $"`{peaks.LowestRatingPro.Value}` <@{peaks.LowestRatingPro.DiscordId}>";

            string highestWinratePro = peaks.HighestWinratePro.Value == 0 ? noData
                : $"`{peaks.HighestWinratePro.Value:0.00%}` <@{peaks.HighestWinratePro.DiscordId}>";

            string mostPlayedPro = peaks.MostPlayedPro.Value == 0 ? noData
                : $"`{peaks.MostPlayedPro.Value}` <@{peaks.MostPlayedPro.DiscordId}>";

            string highestRatingStandard = peaks.HighestRatingStandard.Value == 0 ? noData
                : $"`{peaks.HighestRatingStandard.Value}` <@{peaks.HighestRatingStandard.DiscordId}>";

            string lowestRatingStandard = peaks.LowestRatingStandard.Value is 0 or double.MaxValue ? noData
                : $"`{peaks.LowestRatingStandard.Value}` <@{peaks.LowestRatingStandard.DiscordId}>";

            string highestWinrateStandard = peaks.HighestWinrateStandard.Value == 0 ? noData
                : $"`{peaks.HighestWinrateStandard.Value:0.00%}` <@{peaks.HighestWinrateStandard.DiscordId}>";

            string mostPlayedStandard = peaks.MostPlayedStandard.Value == 0 ? noData
                : $"`{peaks.MostPlayedStandard.Value}` <@{peaks.MostPlayedStandard.DiscordId}>";

            #endregion

            embed.AddField("⠀", $"""
                                 ```
                                 {label}```
                                 """)
                .AddField("Matches", $"""
                                      ```ml
                                      Pro:      {pro}
                                      Standard: {standard}
                                      Other:    {other}
                                      Total:    {matches.Count}```
                                      """, true)
                .AddField("Matches", $"""
                                      ```ml
                                      Today:       {today}
                                      Yesterday:   {yesterday}
                                      This Month:  {thisMonth}
                                      This Season: {thisSeason}```
                                      """, true)
                .AddField("Members", $"""
                                      ```ml
                                      Pro:      {usersPro}
                                      Standard: {usersStandard}
                                      Total:    {users.Count}
                                      Active:   {active}```
                                      """, true)
                .AddField("Draft", $"""

                                    Average Draft: {(timedDrafts > 0 ? $"**{average.Humanize(precision: 2, minUnit: TimeUnit.Second)}** | **{timedDrafts}** Drafts" : "**N/A**")}
                                    Longest Draft: {longest?.LongestUser?.Mention ?? "**N/A**"} {(longest?.HasLongestLink ?? false ? longest.LongestLink.DiscordLink(longest.LongestTime.Humanize(precision: 2, minUnit: TimeUnit.Second)) : string.Empty)}
                                    Shortest Draft: {shortest?.ShortestUser?.Mention ?? "**N/A**"} {(shortest?.HasShortestLink ?? false ? shortest.ShortestLink.DiscordLink(shortest.ShortestTime.Humanize(precision: 2, minUnit: TimeUnit.Second)) : string.Empty)}
                                    """)
                .AddField("Pro", $"""

                                  ⮙ Rating: {highestRatingPro}
                                  ⮛ Rating: {lowestRatingPro}
                                  ⮙ Winrate: {highestWinratePro}
                                  ⮙ Played: {mostPlayedPro}
                                  """, true)
                .AddField("Standard", $"""

                                       ⮙ Rating: {highestRatingStandard}
                                       ⮛ Rating: {lowestRatingStandard}
                                       ⮙ Winrate: {highestWinrateStandard}
                                       ⮙ Played: {mostPlayedStandard}
                                       """, true)
                ;
        }

        private record Peak(ulong DiscordId, double Value);
        private record Peaks(
            Peak HighestRatingPro,
            Peak LowestRatingPro,
            Peak HighestWinratePro,
            Peak MostPlayedPro,

            Peak HighestRatingStandard,
            Peak LowestRatingStandard,
            Peak HighestWinrateStandard,
            Peak MostPlayedStandard
        );

        private static Peaks GetPeaks(IEnumerable<User> users, string[] seasons)
        {
            Peak highest = new(0, 0);
            Peak highestStd = new(0, 0);
            Peak lowest = new(0, double.MaxValue);
            Peak lowestStd = new(0, double.MaxValue);
            Peak mostPlayed = new(0, 0);
            Peak mostPlayedStd = new(0, 0);
            Peak highestWinrate = new(0, 0);
            Peak highestWinrateStd = new(0, 0);

            foreach (User user in users)
            {
                _PeakAnalytics(user, ref highest, ref lowest, ref mostPlayed, ref highestWinrate, ref highestStd,
                    ref lowestStd, ref mostPlayedStd, ref highestWinrateStd, seasons);
            }

            return new Peaks(highest, lowest, highestWinrate, mostPlayed,
                highestStd, lowestStd, highestWinrateStd, mostPlayedStd);
        }

        private static void _PeakAnalytics(User user,
            ref Peak highest,
            ref Peak lowest,
            ref Peak mostPlayed,
            ref Peak highestWinrate,
            ref Peak highestStd,
            ref Peak lowestStd,
            ref Peak mostPlayedStd,
            ref Peak highestWinrateStd,
            string[] seasons)
        {
            //Pro
            List<Domain.Entities.Users.Stats> validStatsPro = user.SeasonStats.Where(s =>
                    seasons.Any(ss => ss == s.Season) && s.LatestSnapshot?.GamesPlayed_Pro > 25)
                .ToList();

            if (validStatsPro.Count != 0)
            {
                double maxEosRating = validStatsPro.Max(s => s.LatestSnapshot?.Rating ?? 0);
                if (maxEosRating > highest.Value) highest = new Peak(user.DiscordId, maxEosRating);

                double minEosRating = validStatsPro.Min(s => s.LatestSnapshot?.Rating ?? double.MaxValue);
                if (minEosRating < lowest.Value) lowest = new Peak(user.DiscordId, minEosRating);

                int mostEosPlayed = validStatsPro.Sum(s => s.LatestSnapshot?.GamesPlayed_Pro ?? 0);
                if (mostEosPlayed > mostPlayed.Value) mostPlayed = new Peak(user.DiscordId, mostEosPlayed);

                double highestEosWinrate = validStatsPro.Max(s => s.LatestSnapshot?.WinRate_Pro ?? 0);
                if (highestEosWinrate > highestWinrate.Value) highestWinrate = new Peak(user.DiscordId, highestEosWinrate);
            }

            //Standard
            List<Domain.Entities.Users.Stats> validStatsStandard = user.SeasonStats.Where(s =>
                    seasons.Any(ss => ss == s.Season) && s.LatestSnapshot?.GamesPlayed_Standard > 25)
                .ToList();

            // ReSharper disable once InvertIf
            if (validStatsStandard.Count != 0)
            {
                double maxEosRating = validStatsStandard.Max(s => s.LatestSnapshot?.Rating_Standard ?? 0);
                if (maxEosRating > highestStd.Value) highestStd = new Peak(user.DiscordId, maxEosRating);

                double minEosRating = validStatsStandard.Min(s => s.LatestSnapshot?.Rating_Standard ?? double.MaxValue);
                if (minEosRating < lowestStd.Value) lowestStd = new Peak(user.DiscordId, minEosRating);

                int mostEosPlayed = validStatsStandard.Sum(s => s.LatestSnapshot?.GamesPlayed_Standard ?? 0);
                if (mostEosPlayed > mostPlayedStd.Value) mostPlayedStd = new Peak(user.DiscordId, mostEosPlayed);

                double highestEosWinrate = validStatsStandard.Max(s => s.LatestSnapshot?.WinRate_Standard ?? 0);
                if (highestEosWinrate > highestWinrateStd.Value) highestWinrateStd = new Peak(user.DiscordId, highestEosWinrate);
            }
        }
    }
}
