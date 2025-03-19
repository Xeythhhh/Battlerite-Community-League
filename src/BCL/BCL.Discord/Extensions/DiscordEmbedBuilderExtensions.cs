using BCL.Common.Extensions;
using BCL.Discord.Components.Draft;
using BCL.Domain.Entities.Analytics;
using BCL.Domain.Entities.Matches;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus.Entities;

namespace BCL.Discord.Extensions;
public static class DiscordEmbedBuilderExtensions
{
    public static DiscordEmbedField GetField(this DiscordEmbedBuilder embedBuilder, DiscordMatch.EmbedStructure field)
    {
        if (field is DiscordMatch.EmbedStructure.Description) throw new ArgumentException("Description is not a field");
        return embedBuilder.Fields[(int)field - 1];
    }

    public static DiscordEmbedBuilder AddMatchHistory(this DiscordEmbedBuilder embedBuilder,
        DiscordUser discordUser,
        User user,
        IEnumerable<Match> matches,
        IMapRepository mapRepository,
        League league)
    {
        (string outcomes, string links, string timeStamps, string eloChange) = GetMatchHistoryValues(discordUser, matches, league, 5, mapRepository, user);

        embedBuilder
            .AddField("⠀", $"""
                            ```
                            {eloChange}{league}
                            ```
                            """)
            .AddField("Outcome", outcomes, true)
            .AddField("Link", links.Trim(), true)
            .AddField("Date", timeStamps, true);

        return embedBuilder;
    }

    private static (string, string, string, string) GetMatchHistoryValues(
        DiscordUser discordUser,
        IEnumerable<Match> matches,
        League league,
        int count,
        IMapRepository mapRepository,
        User user)
    {
        string links = "";
        string outcomes = "";
        string timeStamps = "";
        int totalChange = 0;

        matches.Where(m => m.League == league)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .ToList()
            .ForEach(m =>
            {
                Match.Side side = m.GetSide(discordUser);
                StatsSnapshot? snapshot;

                //TODO FIX STATS DUPLICATION LIKELY CAUSED BY PREMADE TEAMS COMMANDS OR QUEUE
                try
                {
                    snapshot = user.GetStats(m.Season ?? "Old matches do not support seasons yikes")?
                        .Snapshots.FirstOrDefault(s => s.MatchId == m.Id);
                }
                catch (Exception e)
                {
                    Console.WriteLine("This is likely due to a bug with premade teams, attempting to correct it.");
                    Console.WriteLine(e);

                    List<Stats> brokenStats = user.SeasonStats.Where(s => s.Season == m.Season).ToList();
                    if (brokenStats.Count > 1)
                    {
                        Stats real = brokenStats.Single(s => s.Snapshots.Count != 0);
                        brokenStats.Remove(real);
                        foreach (Stats? brokenStat in brokenStats) user.SeasonStats.Remove(brokenStat);

                        snapshot = real.Snapshots.FirstOrDefault(s => s.MatchId == m.Id);
                    }
                    else
                    {
                        throw new Exception($"Match history stat duplication error correction attempt failed @Xeyth ({brokenStats.Count}/{user.SeasonStats.Count})");
                    }
                }

                bool supportsModifiers = snapshot is not null;

                double eloShift = Math.Round(Math.Abs(snapshot?.Eloshift ?? m.EloShift)); //backwards compatibility

                string outcome;
                switch (side)
                {
                    case Match.Side.Team1 when m.Outcome is MatchOutcome.Team1:
                    case Match.Side.Team2 when m.Outcome is MatchOutcome.Team2:
                        totalChange += (int)eloShift;
                        outcome = $"+ {eloShift:00}";
                        break;

                    case Match.Side.Team1 when m.Outcome is MatchOutcome.Team2:
                    case Match.Side.Team2 when m.Outcome is MatchOutcome.Team1:
                        totalChange -= (int)eloShift;
                        outcome = $"- {eloShift:00}";
                        break;

                    default:
                        outcome = "Drop";
                        break;
                }

                if (supportsModifiers && m.Outcome is not MatchOutcome.Canceled)
                    outcome += $" (base {Math.Abs(m.EloShift)})";

                Domain.Entities.Queue.Map map = mapRepository.GetById(m.MapId)!;
                string label = map.Name;

                string timeSince = m.CreatedAt.DiscordTime(DiscordTimeFlag.R);
                string link = m.HasJumpLink
                    ? $"{m.JumpLink.DiscordLink(label, m.Id.ToString()!)}"
                    : $"{label}";

                links += $"`{m.Region.ToString()[..2]}` {link}\n";
                outcomes += $"`{m.GetSide(discordUser)} | {outcome}`\n";
                timeStamps += $"{timeSince}\n";
            });

        string eloChange = $"{(totalChange > 0 ? "+" : string.Empty)}{(totalChange != 0 ? totalChange : string.Empty)}{(totalChange != 0 ? " | " : string.Empty)}";

        return (outcomes, links, timeStamps, eloChange);
    }
}
