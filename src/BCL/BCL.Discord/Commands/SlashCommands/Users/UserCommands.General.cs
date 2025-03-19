using System.Globalization;
using System.Linq.Expressions;

using BCL.Common.Extensions;
using BCL.Core;
using BCL.Discord.Bot;
using BCL.Discord.Extensions;
using BCL.Domain.Entities.Analytics;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;

using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

using Humanizer;
using Humanizer.Localisation;

using Newtonsoft.Json;

using QuickChart;

namespace BCL.Discord.Commands.SlashCommands.Users;
public partial class UserCommands
{
    private static async Task _SendProfileMessage(BaseContext context, DiscordUser discordUser, User user, string[] seasons, DiscordEngine discordEngine)
    {
        await context.FollowUpAsync(new DiscordFollowupMessageBuilder()
            .WithContent($"Generating profile embed for {user.Mention}..."));

        try
        {
            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(await GetProfileEmbedAsync(discordUser, user, seasons, discordEngine))
                .AddComponents(BuildFilterSelectComponent(user.Id, context.User.Id, context.Guild.Emojis, seasons,
                    SelectOption.GetOptions(user)))
            );
        }
        catch (Exception e)
        {
            await discordEngine.Log(e, $"Sending **Profile Embed** for {user.Mention} threw an exception. (in {context.Channel.Mention})", false, true);
        }
    }

    public static async Task<DiscordEmbedBuilder> GetProfileEmbedAsync(DiscordUser discordUser, User user, string[] seasons, DiscordEngine discordEngine)
    {
        List<Domain.Entities.Users.Stats> stats = [.. user.SeasonStats.Where(ss =>
                seasons.Any(s => s == ss.Season)
                && ss.Snapshots.Count != 0) //todo fix Condition needed due to a bug with premade teams, refer to MatchHistory
            .OrderBy(s => s.RecordedAt)];

        //TODO remove when bug is fixed
        try
        {
            List<Domain.Entities.Users.Stats> brokenStats = stats.Where(s => stats.Count(s1 => s1.Season == s.Season) > 1).ToList();
            if (brokenStats.Count != 0)
            {
                Domain.Entities.Users.Stats real = brokenStats.Single(s => s.Snapshots.Count != 0);
                brokenStats.Remove(real);
                foreach (Domain.Entities.Users.Stats? brokenStat in brokenStats) user.SeasonStats.Remove(brokenStat);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        League snapshotType = user.Pro ? League.Pro : League.Standard;
        List<StatsSnapshot> snapshots = stats.SelectMany(stat =>
            stat.Snapshots.Where(snapshot => snapshot.League == snapshotType || user.DisplayBothCharts)
                .OrderBy(snapshot => snapshot.RecordedAt)).ToList();

        //embed.AddField("⠀", "⠀"); // :^) 
        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithAuthor($"{user.InGameName} - {string.Join("|", seasons)}", null, discordUser.AvatarUrl)
            .WithFooter($"Profile version: {user.ProfileVersion}", discordEngine.Guild.IconUrl)
            .WithColor(new DiscordColor(user.ProfileColor))
            .AddField("User Info", GetUserInfoField(user, stats), user.Vip);

        if (user.Vip) embed.AddField("Supporter Info", GetSupporterInfoField(user, discordEngine), true);
        embed.AddField("Draft Info", GetDraftInfoField(user, stats));
        if (!string.IsNullOrEmpty(user.Bio)) embed.AddField("Bio", $"```{user.Styling}\n{user.Bio}```");

        (string melee, string ranged, string support) = GetChampionPoolFields(user);
        embed.AddField("Melee", melee, true)
            .AddField("Ranged", ranged, true)
            .AddField("Support", support, true)
            .AddField("Last Session", GetSessionInfoField(user), true);

        if (user.DisplayBothCharts)
        {
            embed.AddField("Standard League", GetStandardInfoField(user, stats, snapshots), true)
                .AddField("Pro League", GetProInfoField(user, stats, snapshots), true);
        }
        else
        {
            if (user.Pro) embed.AddField("Pro League", GetProInfoField(user, stats, snapshots), true);
            else embed.AddField("Standard League", GetStandardInfoField(user, stats, snapshots), true);
        }

        if (stats.Any(s => s.Disclaimer is not null))
        {
            string disclaimer = stats.Where(s => s.Disclaimer is not null)
                .Aggregate("", (current, stat) => $"{current}\n{stat.Disclaimer}");
            embed.AddField("Disclaimer", disclaimer);
        }

        if (stats.Sum(s => s.LatestSnapshot?.GamesPlayed ?? 0) == 0)
        {
            embed.AddField("Error ⚠️", $"""

                                        ```diff
                                        -User has not played in `{string.Join('|', seasons)}` ⚠️
                                        ```
                                        """);
        }

        #region ChartFile

        Chart chart = GetProfileChart(user, snapshots);

        string? exceptionDetails = (snapshots.Count != 0) ? null : $"""
                                                                    ```diff
                                                                    -{user.InGameName} did not play in {string.Join("|", seasons)}```
                                                                    """;
        string filename = $"Chart_{user.DiscordId}_{DateTime.UtcNow}.png";
        DiscordMessage? fileHostMessage = null;
        if (snapshots.Count != 0)
        {
            try
            {
                byte[] chartBytes = chart.ToByteArray();

                await using MemoryStream memoryStream = new(chartBytes);
                fileHostMessage = await discordEngine.AttachmentsChannel
                    .SendMessageAsync(new DiscordMessageBuilder()
                        .AddFile(filename, memoryStream));

                await memoryStream.DisposeAsync();
            }
            catch (Exception e)
            {
                exceptionDetails = $"""
                                    {e.Source}:
                                    ```diff
                                    - {e.Message}
                                    ```
                                    """;
                await discordEngine.Log(e, exceptionDetails);
            }
        }

        if (fileHostMessage is not null) embed.WithImageUrl(fileHostMessage.Attachments[0].Url); //?? "https://i.imgur.com/a2YnFAd.jpeg" cool winXp task failed succesfully picture
        if (!string.IsNullOrWhiteSpace(exceptionDetails)) embed.AddField("Error⚠️", exceptionDetails);

        #endregion

        return embed;
    }

    private static string GetUserInfoField(User user, IReadOnlyCollection<Domain.Entities.Users.Stats> stats)
    {
        string latestMatch = user.HasLatestMatchLink
            ? user.Vip
                ? user.LatestMatchLink.DiscordLink(user.LatestMatch_DiscordLink_Label, user.LatestMatch_DiscordLink_ToolTip)
                : user.LatestMatch_DiscordLink_Label
            : "Not a gamer :zzz:";

        int won = stats.Sum(s => s.LatestSnapshot?.GamesWon ?? 0);
        int played = stats.Sum(s => s.LatestSnapshot?.GamesPlayed ?? 0);

        return $"""
                {user.Mention} `{(user.Approved ? user.Pro ? "Pro" : "Standard" : "Pending")} {user.Server}{(user.CrossRegion ? "+" : string.Empty)}`
                Winrate: `{$"{(double)won / played:0.00%} ({won}/{played})"}[{user.WinStreak}]`
                Last Queued: {user.LastQueued?.DiscordTime(DiscordTimeFlag.R)}
                Registered: {user.CreatedAt.DiscordTime(DiscordTimeFlag.D)}
                Latest: {latestMatch}

                """;
    }
    private static string GetSupporterInfoField(User user, DiscordEngine discordEngine)
    {
        DiscordEmoji? emoji = user.EmojiId is 0 ? null
            : discordEngine.Guild.Emojis[user.EmojiId];
        string emojiField = user.EmojiId is 0 ? "**N/A**"
            : $"`{emoji!.GetDiscordName()}` {emoji}";

        string team = string.IsNullOrWhiteSpace(user.TeamName) ? "N/A" : user.TeamName;

        return user.Vip
            ? $"""
               Channel: {user.ChannelMention}
               Supporter: {user.RoleMention}
               Team: **{team}**
               Emoji: {emojiField}
               Style: **{user.Styling}**
               """
            : "Not a bcl supporter.";
    }
    private static string GetDraftInfoField(User user, IReadOnlyCollection<Domain.Entities.Users.Stats> stats)
    {
        Domain.Entities.Users.Stats? longest = stats.MaxBy(s => s.LongestDraftTime);
        TimeSpan? longestTime = longest?.LongestDraftTime;
        Uri? longestLink = longest?.LongestDraftLink;
        bool hasLongestLink = longest?.HasLongestLink ?? false;

        Domain.Entities.Users.Stats? shortest = stats.Where(s => s.ShortestDraftTime > TimeSpan.Zero).MinBy(s => s.ShortestDraftTime);
        TimeSpan? shortestTime = shortest?.ShortestDraftTime;
        Uri? shortestLink = shortest?.ShortestDraftLink;
        bool hasShortestLink = shortest?.HasShortestLink ?? false;

        int timedDrafts = stats.Sum(s => s.TimedDrafts);
        TimeSpan average = timedDrafts > 0
            ? new TimeSpan(Convert.ToInt64(
                stats.SelectMany(s => Enumerable.Repeat(s.AverageDraftTime, s.TimedDrafts))
                    .Average(s => s.Ticks)))
            : TimeSpan.Zero;

        string longestDraftLabel = hasLongestLink
            ? $"**{longestTime?.Humanize(precision: 2, minUnit: TimeUnit.Second).Replace(" ", " ** ")}"
            : "**N/A**";
        string longestDraft = user.Vip && hasLongestLink
            ? longestLink!.DiscordLink(longestDraftLabel)
            : longestDraftLabel;

        string shortestDraftLabel = hasShortestLink
            ? $"**{shortestTime?.Humanize(precision: 2, minUnit: TimeUnit.Second).Replace(" ", " ** ")}"
            : "**N/A**";
        string shortestDraft = user.Vip && hasShortestLink
            ? shortestLink!.DiscordLink(shortestDraftLabel)
            : shortestDraftLabel;

        string averageDraftTime = timedDrafts > 0
            ? $"**{average.Humanize(precision: 3).Replace(" ", " ** ")} in **{timedDrafts}** drafts"
            : "**N/A**";

        int captainWins = stats.Sum(s => s.LatestSnapshot?.CaptainWins ?? 0);
        int captainGames = stats.Sum(s => s.LatestSnapshot?.CaptainGames ?? 0);
        string captainWinrate = captainGames > 0 ? $"**{(double)captainWins / captainGames:0.00%}**" : "**N/A**";

        return $"""
                Captain Winrate: {captainWinrate}
                `Longest`: {longestDraft} `Shortest`: {shortestDraft}
                `Average`: {averageDraftTime}
                """;
    }
    private static (string, string, string) GetChampionPoolFields(User user)
    {
        int meleeLines = user.DefaultMelee.Split("\n").Length;
        int rangedLines = user.DefaultRanged.Split("\n").Length;
        int supportLines = user.DefaultSupport.Split("\n").Length;
        int maxLines = Math.Max(meleeLines, Math.Max(rangedLines, supportLines));
        const string empty = "\n ";

        string melee = $"""
                     ```{user.Styling}
                     {user.DefaultMelee}{string.Concat(Enumerable.Repeat(empty, maxLines - meleeLines))}```
                     """;
        string ranged = $"""
                      ```{user.Styling}
                      {user.DefaultRanged}{string.Concat(Enumerable.Repeat(empty, maxLines - rangedLines))}```
                      """;
        string support = $"""
                       ```{user.Styling}
                       {user.DefaultSupport}{string.Concat(Enumerable.Repeat(empty, maxLines - supportLines))}```
                       """;

        return (melee, ranged, support);
    }
    private static string GetSessionInfoField(User user)
    {
        int gamesFormatting = user.DisplayBothCharts ? 7 : 14;
        string games = $"{user.SessionWins}/{user.SessionGames}".FormatForDiscordCode(gamesFormatting, true);
        string winrate = $"{(user.SessionGames > 0 ? (double)user.SessionWins / user.SessionGames : 0):0.00%}".FormatForDiscordCode(7);

        User.BalanceSnapshot[] snapshots = user.BalanceSnapshots.ToArray();
        User.BalanceSnapshot firstTransactionOfSession = snapshots.OrderByDescending(s => s.CreatedAt)
            .Where(s => s.Info.Contains("Match Payout", StringComparison.CurrentCultureIgnoreCase))
            .ElementAtOrDefault(user.SessionGames - 1) ?? user.BalanceSnapshots.Last();

        double balance = snapshots.ToArray()[Array.IndexOf(snapshots, firstTransactionOfSession)..].Sum(s => s.Amount);

        string change = user.DisplayBothCharts
            ? $"S{user.SessionEloChange.FormatForDiscordCode(5, true)}  P{user.SessionEloChange_Pro.FormatForDiscordCode(5, true)}"
            : user.Pro
                ? $"{user.SessionEloChange_Pro}"
                : $"{user.SessionEloChange}";

        int balanceFormatting = user.DisplayBothCharts ? 14 : 22;

        string sessionInfo = $"""
                           ```{user.Styling}
                           {winrate}{games}
                           {change}

                           {balance.ToGuildCurrencyString().FormatForDiscordCode(balanceFormatting, true)}
                           ```
                           """;
        return sessionInfo;
    }
    private static string GetStandardInfoField(User user, IReadOnlyCollection<Domain.Entities.Users.Stats> stats,
        IReadOnlyCollection<StatsSnapshot> snapshots) =>
        GetLeagueInfoField(user, stats, snapshots,
            s => s.LatestSnapshot!.GamesWon_Standard,
            s => s.LatestSnapshot!.GamesPlayed_Standard,
            s => s.Rating_Standard,
            u => u.WinStreak_Standard);
    private static string GetProInfoField(User user,
        IReadOnlyCollection<Domain.Entities.Users.Stats> stats,
        IReadOnlyCollection<StatsSnapshot> snapshots) =>
        GetLeagueInfoField(user, stats, snapshots,
            s => s.LatestSnapshot!.GamesWon_Pro,
            s => s.LatestSnapshot!.GamesPlayed_Pro,
            s => s.Rating,
            u => u.WinStreak_Pro);
    private static string GetLeagueInfoField(User user,
        IReadOnlyCollection<Domain.Entities.Users.Stats> stats,
        IReadOnlyCollection<StatsSnapshot> snapshots,
        Expression<Func<Domain.Entities.Users.Stats, int>> wonExpression,
        Expression<Func<Domain.Entities.Users.Stats, int>> playedExpression,
        Expression<Func<StatsSnapshot, double>> ratingExpression,
        Expression<Func<User, int>> streakExpression)
    {
        int won = stats.Sum(s => wonExpression.Compile()(s));
        int played = stats.Sum(s => playedExpression.Compile()(s));
        int matchCountFormatting = user.DisplayBothCharts ? 6 : 14;
        string matchCount = $"{won}/{played}".FormatForDiscordCode(matchCountFormatting, true);
        string winrate = ((double)won / played).ToString("0.00%").FormatForDiscordCode(8);

        const string noData = "-----";
        double maxValue = snapshots.Count != 0 ? snapshots.Max(s => ratingExpression.Compile()(s)) : 0d;
        double minValue = snapshots.Any(s => ratingExpression.Compile()(s) != 0d)
            ? snapshots.Where(s => ratingExpression.Compile()(s) != 0d)
                .Min(s => ratingExpression.Compile()(s)) : 0d;
        double currentValue = snapshots.Count != 0 ? ratingExpression.Compile()(snapshots.Last()) : 0d;

        int minFormatting = user.DisplayBothCharts ? 7 : 15;
        int currentFormatting = user.DisplayBothCharts ? 11 : 19;
        int streakFormatting = user.DisplayBothCharts ? 7 : 15;
        string max = $"⮙{(maxValue == 0d ? noData : maxValue)}".FormatForDiscordCode(6);
        string min = $"⮛{(minValue == 0d ? noData : minValue)}".FormatForDiscordCode(minFormatting, true);
        string current = (currentValue == 0d ? noData : $"{currentValue}").FormatForDiscordCode(currentFormatting, true);

        return $"""
                ```{user.Styling}
                {winrate}{matchCount}
                MMR{current}
                {max}{min}
                Streak:{streakExpression.Compile()(user).FormatForDiscordCode(streakFormatting, true)}
                ```
                """;
    }
    #region Chart Stuff

    // ReSharper disable once InconsistentNaming
    internal record UserChartSnapshot
    {
        public UserChartSnapshot(
            double standard,
            double winrateStandard,
            double pro,
            double winratePro,
            double winrateTrounament,
            string season)
        {
            Standard = standard is 0d
                ? "'0'"
                : $"'{standard.ToString(CultureInfo.InvariantCulture)}'";
            WinrateStandard = $"'{winrateStandard.ToString(CultureInfo.InvariantCulture)}'";
            Pro = pro is 0d
                ? "'0'"
                : $"'{pro.ToString(CultureInfo.InvariantCulture)}'";
            WinratePro = $"'{winratePro.ToString(CultureInfo.InvariantCulture)}'";
            WinrateTrounament = $"'{winrateTrounament.ToString(CultureInfo.InvariantCulture)}'";
            Season = season;
        }

        public string Standard { get; init; }
        public string WinrateStandard { get; init; }
        public string Pro { get; init; }
        public string WinratePro { get; init; }
        public string WinrateTrounament { get; init; }
        public string Season { get; init; }

        public void Deconstruct(
            out string standard,
            out string winrateStandard,
            out string pro,
            out string winratePro,
            out string winrateTrounament,
            out string season)
        {
            standard = Standard;
            winrateStandard = WinrateStandard;
            pro = Pro;
            winratePro = WinratePro;
            winrateTrounament = WinrateTrounament;
            season = Season;
        }
    }
    private static Chart GetProfileChart(User user, IReadOnlyCollection<StatsSnapshot> rawSnapshots)
    {
        List<UserChartSnapshot> snapshots = GetSnapshots(rawSnapshots);
        string title = $"{JsonConvert.ToString(user.InGameName).Trim('"')} - {string.Join(',', rawSnapshots.Select(s => s.Season).Distinct())} | Profile Version: {user.ProfileVersion} | Bot Version: {CoreConfig.Version}{(snapshots.Count != rawSnapshots.Count ? $" | Interpolated({rawSnapshots.Count}>{snapshots.Count})" : $" ({snapshots.Count})")}";

        string config = $$"""
{
    type: 'line',
    data: { 
        labels: [{{string.Join(", ", snapshots.Select(_ => "'⠀'"))}}], 
        datasets: [{{GetDataSets(user, snapshots)}}], },
    options: {
        annotation: { annotations: [{{GetAnnotations(snapshots)}}] },
        plugins: { 
            datalabels: {
                align: 'top',
                backgroundColor: 'rgba(0,0,0,0.4)',
                color: 'rgba(255,255,255,1)',
                formatter: (value) => {
                    return Math.floor(value);
                },
                display: function(context){
                    var index = context.dataIndex;
                    var value = context.dataset.data[index];
                    return ([{{string.Join(',', GetInflectionPoints(snapshots))}}].some(v => v == value) &&
                            value != context.dataset.data[index - 1] &&
                            value >= 100)
                            ? 'auto'
                            : false;
                },
            },
        },
        responsive: true,
        legend: {
            position: 'right'
        },
        title: {
            display: true,
            text: '{{title}}',
        },
        tooltips: {
            mode: 'index',
        },
        hover: {
            mode: 'index',
        },
        scales: {
            xAxes: [
                {
                    id: 'xSnapshots',
                    scaleLabel: {
                        display: false,
                        labelString: 'Snapshots',
                    },
                },
            ],
            yAxes: [
                {
                    id: 'yRating',
                    position: 'left',
                    scaleLabel: {
                        display: true,
                        labelString: 'Rating',
                    },
                },
                {
                    id: 'yWinRate',
                    position: 'right',
                    gridLines: {
                        drawBorder: false,
                        color: ['green', 'grey', 'grey', 'grey', 'red']
                    },
                    scaleLabel: {
                        display: true,
                        labelString: 'Winrate',
                    },
                    min: 0,
                    max: 100,
                    ticks: {
                        stepSize: 25
                    }
                },
            ],
        },
    },
}
""";
        return new Chart
        {
            Width = 1000,
            BackgroundColor = $"rgba(0,0,0, {user.ChartAlpha})",
            Config = config
        };
    }
    private static List<UserChartSnapshot> GetSnapshots(IReadOnlyCollection<StatsSnapshot> rawSnapshots) =>
        rawSnapshots.Count < 250
            ? ProcessSnapshots(rawSnapshots)
            : CompressSnapshots(rawSnapshots);
    private static string GetDataSets(User user, IReadOnlyCollection<UserChartSnapshot> chartSnapshots)
    {
        List<string> validSeasons = chartSnapshots.Select(s => s.Season).Distinct().ToList();

        const string gradientFill = "'rgba(45, 45, 45, 0.1)'";
        const string tournamentWinrateColor = "#E01C5C5E"; //Not customizable

        bool displayPro = user.Pro && user.SeasonStats
            .Any(s => s.LatestSnapshot?.GamesPlayed_Pro > 0 && validSeasons.Any(v => v == s.Season)); //this might troll early seasons

        bool displayStandard = (!displayPro || user.DisplayBothCharts) &&
                user.SeasonStats.Any(s => s.LatestSnapshot?.GamesPlayed_Standard > 0 && validSeasons.Any(v => v == s.Season));

        bool displayTourney = user.SeasonStats
            .Any(s => s.LatestSnapshot?.GamesWon_Tournament > 0 && validSeasons.Any(v => v == s.Season));

        #region Templates

        string proRating = $$"""
                          
                                      {
                                          label: 'Rating (Pro)',
                                          fill: false,
                                          pointRadius: 0,
                                          borderColor: '{{user.Chart_MainRatingColor}}',
                                          data: [{{string.Join(", ", chartSnapshots.Select(s => s.Pro))}}],
                                          yAxisID: 'yRating'
                                      },
                          """;
        string proWinrate = $$"""
                           
                                       {
                                           label: 'Winrate (Pro)',
                                           fill: 'origin',
                                           origin: '50',
                                           pointRadius: 0,
                                           borderColor: '{{user.Chart_MainWinrateColor}}',
                                           backgroundColor: getGradientFillHelper('vertical', [{{gradientFill}}, {{gradientFill}}, '{{user.Chart_MainWinrateColor}}', '{{user.Chart_MainWinrateColor}}', {{gradientFill}}, {{gradientFill}}]),
                                           data: [{{string.Join(", ", chartSnapshots.Select(s => s.WinratePro))}}],
                                           yAxisID: 'yWinRate'
                                       },
                           """;
        string standardRating = $$"""
                               
                                           {
                                               label: 'Rating (Standard)',
                                               fill: false,
                                               pointRadius: 0,
                                               borderColor: '{{user.Chart_SecondaryRatingColor}}',
                                               data: [{{string.Join(", ", chartSnapshots.Select(s => s.Standard))}}],
                                               yAxisID: 'yRating'
                                           },
                               """;
        string standardWinrate = $$"""
                                
                                            {
                                                label: 'Winrate (Standard)',
                                                pointRadius: 0,
                                                borderColor: '{{user.Chart_SecondaryWinrateColor}}',
                                                backgroundColor: getGradientFillHelper('vertical', [{{gradientFill}}, {{gradientFill}}, '{{user.Chart_SecondaryWinrateColor}}', {{gradientFill}}, {{gradientFill}}]),
                                                data: [{{string.Join(", ", chartSnapshots.Select(s => s.WinrateStandard))}}],
                                                yAxisID: 'yWinRate'
                                            },
                                """;
        string tournamentWinrate = $$"""
                                  
                                              {
                                                  label: 'Winrate (Tournament)',
                                                  pointRadius: 0,
                                                  borderColor: '{{tournamentWinrateColor}}',
                                                  backgroundColor: getGradientFillHelper('vertical', ['{{tournamentWinrateColor}}', '{{tournamentWinrateColor}}', '{{tournamentWinrateColor}}', '{{tournamentWinrateColor}}', '{{tournamentWinrateColor}}', {{gradientFill}}, {{gradientFill}}]),
                                                  data: [{{string.Join(", ", chartSnapshots.Select(s => s.WinrateTrounament))}}],
                                                  yAxisID: 'yWinRate'
                                              },
                                  """;

        #endregion

        string charts = string.Empty;
        if (displayPro) charts += proRating;
        if (displayStandard) charts += standardRating;
        if (displayPro) charts += proWinrate;
        if (displayStandard) charts += standardWinrate;
        if (displayTourney) charts += tournamentWinrate;

        return charts;
    }
    private static string GetAnnotations(List<UserChartSnapshot> chartSnapshots) =>
        chartSnapshots.Select(s => s.Season).Distinct()
            .Aggregate("", (current, snapshotSeason) =>
            {
                int snapshotIndex = chartSnapshots.FindIndex(s => s.Season == snapshotSeason);
                string position = snapshotIndex switch
                {
                    _ when snapshotIndex < chartSnapshots.Count / 4 => $"{15 * (snapshotSeason.Length / 4)}",
                    _ when snapshotIndex > chartSnapshots.Count / 4 * 3 => $"{-15 * (snapshotSeason.Length / 4)}",
                    _ => "0"
                };
                string value = $$"""
                              {
                                  type: 'line',
                                  mode: 'vertical',
                                  scaleID: 'xSnapshots',
                                  value: '{{snapshotIndex}}',
                                  borderColor: 'rgba(255, 255, 255, 0.2)',
                                  borderWidth: 2,
                                  borderDash: [ 3 ],
                                  label: {
                                      display: 'auto',
                                      enabled: true,
                                      backgroundColor: 'rgba(0,0,0,0)',
                                      content: '{{snapshotSeason}}',
                                      xAdjust: {{position}},
                                      yAdjust: 110,
                                      yValue: 1
                                  },
                              },
                              """;
                return $"{current} {value}";
            }).Trim();

    private static IEnumerable<double> GetInflectionPoints(IReadOnlyCollection<UserChartSnapshot> snapshots) =>
        GetInflectionsForLeague(snapshots, s => s.Pro)
            .Concat(GetInflectionsForLeague(snapshots, s => s.Standard))
            .Where(i => i > 500);
    private static IEnumerable<double> GetInflectionsForLeague(IEnumerable<UserChartSnapshot> snapshots, Expression<Func<UserChartSnapshot, string>> expression) =>
        snapshots.Select(s => (double.Parse(expression.Compile()(s).Trim('\'')), s.Season)).GroupBy(s => s.Season)
            .Select(g => new[] { g.Max(s => s.Item1), g.Min(s => s.Item1) })
            .SelectMany(i => i);
    private static List<UserChartSnapshot> CompressSnapshots(IEnumerable<StatsSnapshot> source)
    {
        List<StatsSnapshot> snapshots = source.ToList();

        #region Offsets

        double winsOffsetPro = 0d;
        double gamesOffsetPro = 0d;
        double winsOffsetStandard = 0d;
        double gamesOffsetStandard = 0d;
        double winsOffsetTournament = 0d;
        double gamesOffsetTournament = 0d;
        string? offsetSeason = snapshots[0].Season;

        #endregion

        int index = 0;
        List<IGrouping<int, StatsSnapshot>> groups = snapshots.GroupBy(_ => index++ / ((int)Math.Ceiling(snapshots.Count / 250d))).ToList();

        return groups.ConvertAll(ProcessGroup);

        UserChartSnapshot ProcessGroup(IGrouping<int, StatsSnapshot> group)
        {
            double pro = group.Average(s => s.Rating);
            double standard = group.Any(s => s.Rating_Standard != 0)
                ? group.Where(s => s.Rating_Standard != 0)
                    .Average(s => s.Rating_Standard) : 0d;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (standard == 0) standard = pro; //old seasons had one rating for both leagues

            double proGames = group.Sum(s => s.GamesPlayed_Pro) + gamesOffsetPro;
            double proWins = group.Sum(s => s.GamesWon_Pro) + winsOffsetPro;
            double proWinrate = proGames > 0
                ? proWins / proGames * 100
                : 0;

            double standardGames = group.Sum(s => s.GamesPlayed_Standard) + gamesOffsetStandard;
            double standardWins = group.Sum(s => s.GamesWon_Standard) + winsOffsetStandard;
            double standardWinrate = standardGames > 0
                ? standardWins / standardGames * 100
                : 0;

            double tourneyGames = group.Sum(s => s.GamesPlayed_Tournament) + gamesOffsetTournament;
            double tourneyWins = group.Sum(s => s.GamesWon_Tournament) + winsOffsetTournament;
            double tourneyWinrate = tourneyGames > 0
                ? tourneyWins / tourneyGames * 100
                : 0;

            string season = group.GroupBy(s => s.Season).OrderByDescending(x => x.Count()).First().Key;

            string? nextSeason = groups.ElementAtOrDefault(groups.IndexOf(group) + 1)?
                .GroupBy(s => s.Season).OrderByDescending(x => x.Count()).First().Key;

            if (nextSeason == offsetSeason)
            {
                return new UserChartSnapshot(standard, standardWinrate,
                    pro, proWinrate,
                    tourneyWinrate, season);
            }

            offsetSeason = nextSeason;
            gamesOffsetPro += proGames;
            winsOffsetPro += proWins;
            gamesOffsetStandard += standardGames;
            winsOffsetStandard += standardWins;
            gamesOffsetTournament += tourneyGames;
            winsOffsetTournament += tourneyWins;

            return new UserChartSnapshot(standard, standardWinrate,
                pro, proWinrate,
                tourneyWinrate, season);
        }
    }
    private static List<UserChartSnapshot> ProcessSnapshots(IEnumerable<StatsSnapshot> source)
    {
        List<StatsSnapshot> snapshots = source.ToList();
        if (snapshots.Count == 0) return [];

        #region Offsets

        double winsOffsetPro = 0d;
        double gamesOffsetPro = 0d;
        double winsOffsetStandard = 0d;
        double gamesOffsetStandard = 0d;
        double winsOffsetTournament = 0d;
        double gamesOffsetTournament = 0d;
        string? offsetSeason = snapshots[0].Season;

        #endregion

        return snapshots.ConvertAll(snap =>
            {
                double pro = snap.Rating;
                double proGames = snap.GamesPlayed_Pro + gamesOffsetPro;
                double proWins = snap.GamesWon_Pro + winsOffsetPro;
                double proWinrate = proGames > 0 ? proWins / proGames * 100 : 0;

                double standard = snap.Rating_Standard > 0 ? snap.Rating_Standard : snap.Rating;
                double standardGames = snap.GamesPlayed_Standard + gamesOffsetStandard;
                double standardWins = snap.GamesWon_Standard + winsOffsetStandard;
                double standardWinrate = standardGames > 0 ? standardWins / standardGames * 100 : 0;

                double tourneyGames = snap.GamesPlayed_Tournament + gamesOffsetTournament;
                double tourneyWins = snap.GamesWon_Tournament + winsOffsetTournament;
                double winrateTrounament = tourneyGames > 0 ? tourneyWins / tourneyGames * 100 : 0;

                StatsSnapshot? next = snapshots.ElementAtOrDefault(snapshots.IndexOf(snap) + 1);
                if (next?.Season == offsetSeason)
                {
                    return new UserChartSnapshot(
                        standard,
                        standardWinrate,
                        pro,
                        proWinrate,
                        winrateTrounament,
                        snap.Season);
                }

                //do offset stff
                offsetSeason = next?.Season;
                gamesOffsetPro += proGames;
                winsOffsetPro += proWins;
                gamesOffsetStandard += standardGames;
                winsOffsetStandard += standardWins;
                gamesOffsetTournament += tourneyGames;
                winsOffsetTournament += tourneyWins;

                return new UserChartSnapshot(
                    standard,
                    standardWinrate,
                    pro,
                    proWinrate,
                    winrateTrounament,
                    snap.Season);
            })
;
    }

    #endregion
}
