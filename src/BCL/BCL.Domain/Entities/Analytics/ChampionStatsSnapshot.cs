// ReSharper disable InconsistentNaming

using System.ComponentModel.DataAnnotations.Schema;

namespace BCL.Domain.Entities.Analytics;
public class ChampionStatsSnapshot : StatsSnapshot
{
    public ChampionStatsSnapshot() { }
    public ChampionStatsSnapshot(ChampionStatsSnapshot? other) : base(other)
    {
        if (other is not null)
        {
            Banned = other.Banned;
            Banned_Custom = other.Banned_Custom;
            Banned_Event = other.Banned_Event;
            Banned_Pro = other.Banned_Pro;
            Banned_Standard = other.Banned_Standard;
            Banned_Tournament = other.Banned_Tournament;

            MatchCount = other.MatchCount;
            MatchCount_Custom = other.MatchCount_Custom;
            MatchCount_Event = other.MatchCount_Event;
            MatchCount_Pro = other.MatchCount_Pro;
            MatchCount_Standard = other.MatchCount_Standard;
            MatchCount_Tournament = other.MatchCount_Tournament;
            MatchCount_Premade3v3 = other.MatchCount_Premade3v3;
        }
    }

    public int MatchCount { get; set; }
    public int MatchCount_Standard { get; set; }
    public int MatchCount_Pro { get; set; }
    public int MatchCount_Custom { get; set; }
    public int MatchCount_Event { get; set; }
    public int MatchCount_Tournament { get; set; }
    public int MatchCount_Premade3v3 { get; set; }

    public int Banned { get; set; }
    [NotMapped] public double BanRate => MatchCount == 0 ? 0 : (double)Banned / MatchCount;
    [NotMapped] public double PickRate => MatchCount == 0 ? 0 : (double)GamesPlayed / MatchCount;

    public int Banned_Standard { get; set; }
    [NotMapped] public double BanRate_Standard => MatchCount_Standard == 0 ? 0 : (double)Banned_Standard / MatchCount_Standard;
    [NotMapped] public double PickRate_Standard => MatchCount_Standard == 0 ? 0 : (double)GamesPlayed_Standard / MatchCount_Standard;

    public int Banned_Pro { get; set; }
    [NotMapped] public double BanRate_Pro => MatchCount_Pro == 0 ? 0 : (double)Banned_Pro / MatchCount_Pro;
    [NotMapped] public double PickRate_Pro => MatchCount_Pro == 0 ? 0 : (double)GamesPlayed_Pro / MatchCount_Pro;

    public int Banned_Tournament { get; set; }
    [NotMapped] public double BanRate_Tournament => MatchCount_Tournament == 0 ? 0 : (double)Banned_Tournament / MatchCount_Tournament;
    [NotMapped] public double PickRate_Tournament => MatchCount_Tournament == 0 ? 0 : (double)GamesPlayed_Tournament / MatchCount_Tournament;

    public int Banned_Event { get; set; }
    [NotMapped] public double BanRate_Event => MatchCount_Event == 0 ? 0 : (double)Banned_Event / MatchCount_Event;
    [NotMapped] public double PickRate_Event => MatchCount_Event == 0 ? 0 : (double)GamesPlayed_Event / MatchCount_Event;

    public int Banned_Custom { get; set; }
    [NotMapped] public double BanRate_Custom => MatchCount_Custom == 0 ? 0 : (double)Banned_Custom / MatchCount_Custom;
    [NotMapped] public double PickRate_Custom => MatchCount_Custom == 0 ? 0 : (double)GamesPlayed_Custom / MatchCount_Custom;

    public int Banned_Premade3v3 { get; set; }
    [NotMapped] public double BanRate_Premade3v3 => MatchCount_Premade3v3 == 0 ? 0 : (double)Banned_Premade3v3 / MatchCount_Premade3v3;
    [NotMapped] public double PickRate_Premade3v3 => MatchCount_Premade3v3 == 0 ? 0 : (double)GamesPlayed_Premade3v3 / MatchCount_Premade3v3;
}
