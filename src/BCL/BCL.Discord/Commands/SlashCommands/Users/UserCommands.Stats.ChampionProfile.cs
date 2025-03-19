using BCL.Common.Extensions;
using BCL.Core;
using BCL.Discord.Bot;
using BCL.Discord.Extensions;
using BCL.Discord.OptionProviders;
using BCL.Domain;
using BCL.Domain.Entities.Analytics;
using BCL.Domain.Entities.Queue;
using BCL.Domain.Enums;

using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

using Newtonsoft.Json;

using QuickChart;

namespace BCL.Discord.Commands.SlashCommands.Users;
public partial class UserCommands
{
    public partial class Stats
    {
        [SlashCommand("ChampionProfile", "Display a champion's stats.")]
        public async Task ChampionProfile(InteractionContext context,
            [Autocomplete(typeof(ChampionAutocompleteProvider))]
            [Option("Champion", "Champion")] string championId)
        {
            await context.CreateResponseAsync("Generating champion profile...");
            Champion? champion = await championRepository.GetByIdAsync(Ulid.Parse(championId));
            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(await GetChampionProfileEmbedAsync(
                    champion!,
                    GetGenericEmbed(context.Guild.IconUrl),
                    [DomainConfig.Season],
                    discordEngine))
                .AddComponents(BuildFilterSelectComponent(
                    Ulid.Parse(championId), context.User.Id, context.Guild.Emojis, [DomainConfig.Season],
                    SelectOption.GetOptions(champion!),
                    SelectFor.ChampionProfile)));
        }

        public static async Task<DiscordEmbedBuilder> GetChampionProfileEmbedAsync(
            Champion champion,
            DiscordEmbedBuilder embed,
            string[] seasons,
            DiscordEngine discordEngine)
        {
            embed.WithTitle($"{champion.Name} - {string.Join('|', seasons)}");

            List<ChampionStats> stats = [.. champion.Stats.Where(s => seasons.Any(ss => ss == s.Season)).OrderBy(s => s.RecordedAt)];

            int played = stats.Sum(s => s.LatestSnapshot?.GamesPlayed ?? 0);
            int won = stats.Sum(s => s.LatestSnapshot?.GamesWon ?? 0);
            int banned = stats.Sum(s => s.LatestSnapshot?.Banned ?? 0);
            int matchCount = stats.Sum(s => s.LatestSnapshot?.MatchCount ?? 0);

            embed.WithDescription($"""

                                   Latest Match: {(champion.HasLatestMatchLink ? champion.LatestMatch.DiscordLink(champion.LatestMatch_Label) : "N/A")}
                                   ```ml
                                   Win:  {((double)won / played).ToString("0.00%").FormatForDiscordCode(7)} {won}
                                   Pick: {((double)played / matchCount).ToString("0.00%").FormatForDiscordCode(7)} {played}
                                   Ban:  {((double)banned / matchCount).ToString("0.00%").FormatForDiscordCode(7)} {banned}
                                   ```
                                   """);
            AddLeagueInfo(embed, stats, League.Pro);
            AddLeagueInfo(embed, stats, League.Standard);
            AddLeagueInfo(embed, stats, League.Tournament);

            List<ChampionStatsSnapshot> rawSnapshots = stats.SelectMany(s => s.Snapshots.OrderBy(snap => snap.RecordedAt)).ToList();
            List<ChampionChartSnapshot> snapshots = ProcessChampionSnapshots(rawSnapshots);

            #region Chart

            Chart chart = new()
            {
                Width = 1000,
                BackgroundColor = "rgba(0,0,0, 0.8)",
                Config = $$"""
                           {
                               type: 'line',
                               data: {
                                   labels: [{{string.Join(", ", snapshots.Select(_ => "'⠀'"))}}],
                                   datasets: [{
                                       label: 'Winrate (Pro)',
                                       pointRadius: 0,
                                       borderColor: 'rgba(39, 245, 108, 0.8)',
                                       backgroundColor: 'rgba(0, 0, 0, 0)',
                                       data: [{{string.Join(", ", snapshots.Select(s => $"'{s.WinRate_Pro}'"))}}],
                                   }, {
                                       label: 'Winrate (Standard)',
                                       pointRadius: 0,
                                       backgroundColor: 'rgba(0, 0, 0, 0)',
                                       borderColor: 'rgba(168, 245, 39, 0.8)',
                                       data: [{{string.Join(", ", snapshots.Select(s => $"'{s.WinRate_Standard}'"))}}],
                                   }, {
                                       label: 'Pickrate',
                                       pointRadius: 0,
                                       borderDash: [5, 5],
                                       borderColor: 'rgba(39, 129, 245, 0.8)',
                                       backgroundColor: 'rgba(0, 0, 0, 0)',
                                       data: [{{string.Join(", ", snapshots.Select(s => $"'{s.PickRate}'"))}}],
                                   }, {
                                       label: 'Banrate',
                                       pointRadius: 0,
                                       borderDash: [5, 5],
                                       backgroundColor: 'rgba(0, 0, 0, 0)',
                                       borderColor: 'rgba(245, 78, 39, 0.8)',
                                       data: [{{string.Join(", ", snapshots.Select(s => $"'{s.BanRate}'"))}}],
                                   },],
                               },
                               options: {
                                   annotation: { annotations: [{{GetAnnotations(snapshots)}}] },
                                   responsive: true,
                                   title: { display: true, text: "{{JsonConvert.ToString(champion.Name).Trim('"')}} - Bot Version: {{CoreConfig.Version}} | {{DateTime.UtcNow:yyyy MMMM dd}}{{(rawSnapshots.Count != snapshots.Count ? " | Interpolated" : string.Empty)}}", },
                               	scales: {
                           	        xAxes:[{
                                               id: 'xSnapshots',
                                               display: false
                                       }],
                           	        yAxes:[
                                           {
                           	                position: 'left',
                           	                scaleLabel: { display: true }
                                           },
                                       ],
                                   },
                               },
                           }
                           """
            };

            string filename = $"Chart_Champion_{champion.Id}_{DateTime.UtcNow}.png";
            try
            {
                byte[] chartBytes = chart.ToByteArray();

                await using MemoryStream memoryStream = new(chartBytes);
                DiscordMessage filehostMessage = await discordEngine.AttachmentsChannel.SendMessageAsync(new DiscordMessageBuilder()
                    .AddFile(filename, memoryStream, true));

                await memoryStream.DisposeAsync();

                embed.WithImageUrl(filehostMessage?.Attachments[0].Url ?? "https://i.imgur.com/a2YnFAd.jpeg");
            }
            catch (Exception e)
            {
                await discordEngine.Log(e, "Generating Wallet Chart");
            }

            #endregion

            return embed;
        }

        // ReSharper disable InconsistentNaming
        public record ChampionChartSnapshot(double WinRate_Pro, double WinRate_Standard, double PickRate, double BanRate, string Season);
        private static List<ChampionChartSnapshot> ProcessChampionSnapshots(IReadOnlyCollection<ChampionStatsSnapshot> rawSnapshots)
        {
            if (rawSnapshots.Count >= 250) return CompressSnapshots(rawSnapshots);

            List<ChampionStatsSnapshot> snapshots = [.. rawSnapshots];

            double winOffset_Pro = 0d;
            double playedOffset_Pro = 0d;

            double winOffset_Standard = 0d;
            double playedOffset_Standard = 0d;

            double pickOffset = 0d;
            double banOffset = 0d;
            double matchCountOffset = 0d;

            string? offsetSeason = snapshots[0].Season;
            // ReSharper restore InconsistentNaming
            return snapshots.ConvertAll(s =>
            {
                double winPro =
                    (s.GamesWon_Pro + winOffset_Pro) /
                    (s.GamesPlayed_Pro + playedOffset_Pro) * 100;

                double winStandard =
                    (s.GamesWon_Standard + winOffset_Standard) /
                    (s.GamesPlayed_Standard + playedOffset_Standard) * 100;

                double pick =
                    (s.GamesPlayed + pickOffset) /
                    (s.MatchCount + matchCountOffset) * 100;

                double ban = (s.Banned + banOffset) /
                    (s.MatchCount + matchCountOffset) * 100;

                ChampionStatsSnapshot? next = snapshots.ElementAtOrDefault(snapshots.IndexOf(s) + 1);
                if (next?.Season == offsetSeason)
                    return new ChampionChartSnapshot(winPro, winStandard, pick, ban, s.Season);

                offsetSeason = next?.Season;
                winOffset_Pro += s.GamesWon_Pro;
                playedOffset_Pro += s.GamesPlayed_Pro;
                winOffset_Standard += s.GamesWon_Standard;
                playedOffset_Standard += s.GamesPlayed_Standard;
                pickOffset += s.GamesPlayed;
                banOffset += s.Banned;
                matchCountOffset += s.MatchCount;

                return new ChampionChartSnapshot(winPro, winStandard, pick, ban, s.Season);
            });
        }
        private static List<ChampionChartSnapshot> CompressSnapshots(IReadOnlyCollection<ChampionStatsSnapshot> snapshots)
        {
            // ReSharper disable InconsistentNaming
            double winOffset_Pro = 0d;
            double playedOffset_Pro = 0d;

            double winOffset_Standard = 0d;
            double playedOffset_Standard = 0d;

            double pickOffset = 0d;
            double banOffset = 0d;
            double matchCountOffset = 0d;

            string? offsetSeason = snapshots.First().Season;
            // ReSharper restore InconsistentNaming

            int index = 0;

            List<IGrouping<int, ChampionStatsSnapshot>> groups = snapshots.GroupBy(_ => index++ / ((int)Math.Ceiling(snapshots.Count / 250d))).ToList();
            List<ChampionChartSnapshot> interpolatedSnapshots = groups.ConvertAll(g =>
                {
                    double wonStandard = g.Average(s => s.GamesWon_Standard);
                    double playedStandard = g.Average(s => s.GamesPlayed_Standard);

                    double wonPro = g.Average(s => s.GamesWon_Pro);
                    double playedPro = g.Average(s => s.GamesPlayed_Pro);

                    double picked = g.Average(s => s.GamesPlayed);
                    double banned = g.Average(s => s.Banned);

                    double matchCount = g.Average(s => s.MatchCount);

                    string season = g.GroupBy(s => s.Season).OrderByDescending(x => x.Count()).First().Key; //kek

                    double winPro =
                        (wonPro + winOffset_Pro) /
                        (playedPro + playedOffset_Pro) * 100;

                    double winStandard =
                        (wonStandard + winOffset_Standard) /
                        (playedStandard + playedOffset_Standard) * 100;

                    double pick =
                        (picked + pickOffset) /
                        (matchCount + matchCountOffset) * 100;

                    double ban = (banned + banOffset) /
                        (matchCount + matchCountOffset) * 100;

                    string? nextSeason = groups.ElementAtOrDefault(groups.IndexOf(g) + 1)?
                        .GroupBy(s => s.Season)
                        .OrderByDescending(x => x.Count())
                        .First().Key;

                    if (nextSeason == offsetSeason) return new ChampionChartSnapshot(winPro, winStandard, pick, ban, season);

                    offsetSeason = nextSeason;
                    winOffset_Pro += wonPro;
                    playedOffset_Pro += playedPro;
                    winOffset_Standard += wonStandard;
                    playedOffset_Standard += playedStandard;
                    pickOffset += picked;
                    banOffset += banned;
                    matchCountOffset += matchCount;

                    return new ChampionChartSnapshot(winPro, winStandard, pick, ban, season);
                })
;
            return interpolatedSnapshots;
        }
        private static void AddLeagueInfo(DiscordEmbedBuilder embed, IReadOnlyCollection<ChampionStats> stats, League league)
        {
            int played = stats.Sum(s =>
            {
                System.Reflection.PropertyInfo? property = s.LatestSnapshot?.GetType().GetProperty($"GamesPlayed_{league}");
                return (int)(property?.GetMethod?.Invoke(s.LatestSnapshot, null) ?? 0);
            });

            int won = stats.Sum(s =>
            {
                System.Reflection.PropertyInfo? property = s.LatestSnapshot?.GetType().GetProperty($"GamesWon_{league}");
                return (int)(property?.GetMethod?.Invoke(s.LatestSnapshot, null) ?? 0);
            });

            int banned = stats.Sum(s =>
            {
                System.Reflection.PropertyInfo? property = s.LatestSnapshot?.GetType().GetProperty($"Banned_{league}");
                return (int)(property?.GetMethod?.Invoke(s.LatestSnapshot, null) ?? 0);
            });

            int matchCount = stats.Sum(s =>
            {
                System.Reflection.PropertyInfo? property = s.LatestSnapshot?.GetType().GetProperty($"MatchCount_{league}");
                return (int)(property?.GetMethod?.Invoke(s.LatestSnapshot, null) ?? 0);
            });

            embed.AddField($"{league}", $"""
                                         ```ml
                                         W {((double)won / played).ToString("0.00%").FormatForDiscordCode(7)} {won}
                                         P {((double)played / matchCount).ToString("0.00%").FormatForDiscordCode(7)} {played}
                                         B {((double)banned / matchCount).ToString("0.00%").FormatForDiscordCode(7)} {banned}
                                         ```
                                         """, true);
        }
        private static string GetAnnotations(List<ChampionChartSnapshot> chartSnapshots) =>
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
                                          yAdjust: 100,
                                          yValue: 1
                                      },
                                  },
                                  """;
                    return $"{current} {value}";
                }).Trim();
    }
}
