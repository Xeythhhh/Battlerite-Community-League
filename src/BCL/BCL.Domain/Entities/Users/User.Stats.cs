using System.ComponentModel.DataAnnotations.Schema;

using BCL.Domain.Entities.Analytics;

// ReSharper disable InconsistentNaming

namespace BCL.Domain.Entities.Users;

public partial class User
{
    /// <summary>
    /// Number of times this user subbed in for an afk and saved the queue
    /// </summary>
    public int SubbedIn { get; set; } = 0;

    public int WinStreak { get; set; } = 0;
    public int WinStreak_Standard { get; set; } = 0;
    public int WinStreak_Pro { get; set; } = 0;
    public int SessionWins { get; set; } = 0;
    public int SessionGames { get; set; } = 0;
    public int SessionEloChange { get; set; } = 0;
    public int SessionEloChange_Pro { get; set; } = 0;
    public int PlacementGamesRemaining { get; set; }
    public int PlacementGamesRemaining_Standard { get; set; }

    public bool IsInPlacements => PlacementGamesRemaining > 0;
    public bool IsInPlacements_Standard => PlacementGamesRemaining_Standard > 0;

    public virtual List<Stats> SeasonStats { get; set; } = [];
    [NotMapped]
    public Stats CurrentSeasonStats
    {
        get
        {
            Stats? value = SeasonStats.FirstOrDefault(ss => ss.Season == DomainConfig.Season);
            if (value is not null) return value;

            value = new Stats
            {
                Season = DomainConfig.Season,
                HighestRating = Rating,
                LowestRating = Rating,
                HighestRating_Standard = Rating_Standard,
                LowestRating_Standard = Rating_Standard,
                Snapshots = []
            };

            SeasonStats.Add(value);
            return value;
        }
    }
    [NotMapped] public StatsSnapshot? LatestSnapshot => CurrentSeasonStats.LatestSnapshot;
    public Stats? GetStats(string season) => SeasonStats.SingleOrDefault(s => s.Season.Equals(season, StringComparison.CurrentCultureIgnoreCase));
}
