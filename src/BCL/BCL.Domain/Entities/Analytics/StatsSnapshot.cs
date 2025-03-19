using BCL.Domain.Entities.Abstract;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;

// ReSharper disable InconsistentNaming
namespace BCL.Domain.Entities.Analytics;
public class StatsSnapshot : Entity
{
    public int GamesPlayed { get; set; }
    public int GamesWon { get; set; }
    public double WinRate => GamesPlayed == 0 ? 0 : (double)GamesWon / GamesPlayed;

    public int GamesPlayed_Standard { get; set; }
    public int GamesWon_Standard { get; set; }
    public double WinRate_Standard => GamesPlayed_Standard == 0 ? 0 : (double)GamesWon_Standard / GamesPlayed_Standard;

    public int GamesPlayed_Pro { get; set; }
    public int GamesWon_Pro { get; set; }
    public double WinRate_Pro => GamesPlayed_Pro == 0 ? 0 : (double)GamesWon_Pro / GamesPlayed_Pro;

    public int GamesPlayed_Tournament { get; set; }
    public int GamesWon_Tournament { get; set; }
    public double WinRate_Tournament => GamesPlayed_Tournament == 0 ? 0 : (double)GamesWon_Tournament / GamesPlayed_Tournament;

    public int GamesPlayed_Event { get; set; }
    public int GamesWon_Event { get; set; }
    public double WinRate_Event => GamesPlayed_Event == 0 ? 0 : (double)GamesWon_Event / GamesPlayed_Event;

    public int GamesPlayed_Custom { get; set; }
    public int GamesWon_Custom { get; set; }
    public double WinRate_Custom => GamesPlayed_Custom == 0 ? 0 : (double)GamesWon_Custom / GamesPlayed_Custom;
    public int GamesPlayed_Premade3v3 { get; set; }
    public int GamesWon_Premade3v3 { get; set; }
    public double WinRatePremade3v3 => GamesPlayed_Premade3v3 == 0 ? 0 : (double)GamesWon_Premade3v3 / GamesPlayed_Premade3v3;

    public Ulid? MatchId { get; set; } = null;
    public double? Eloshift { get; set; } = null;
    public double Rating { get; set; }
    public double Rating_Standard { get; set; }

    public string Season { get; set; } = DomainConfig.Season;

    public int CaptainGames { get; set; }
    public int CaptainWins { get; set; }
    /// <summary>
    /// Please specify the league when creating a new snapshot for proper graphs ty
    /// </summary>
    public League League { get; set; } = League.Custom;

    public StatsSnapshot() { }
    public StatsSnapshot(StatsSnapshot? other)
    {
        if (other is not null)
        {
            Season = other.Season;
            Rating = other.Rating;
            CaptainGames = other.CaptainGames;
            CaptainWins = other.CaptainWins;
            Rating_Standard = other.Rating_Standard;
            GamesPlayed = other.GamesPlayed;
            GamesPlayed_Standard = other.GamesPlayed_Standard;
            GamesPlayed_Custom = other.GamesPlayed_Custom;
            GamesPlayed_Event = other.GamesPlayed_Event;
            GamesPlayed_Pro = other.GamesPlayed_Pro;
            GamesPlayed_Tournament = other.GamesPlayed_Tournament;
            GamesPlayed_Premade3v3 = other.GamesPlayed_Premade3v3;
            GamesWon = other.GamesWon;
            GamesWon_Standard = other.GamesWon_Standard;
            GamesWon_Custom = other.GamesWon_Custom;
            GamesWon_Event = other.GamesWon_Event;
            GamesWon_Pro = other.GamesWon_Pro;
            GamesWon_Tournament = other.GamesWon_Tournament;
            GamesWon_Premade3v3 = other.GamesWon_Premade3v3;
        }
    }
    public StatsSnapshot(User user)
    {
        StatsSnapshot? latest = user.CurrentSeasonStats.LatestSnapshot;
        if (latest is not null)
        {
            Season = latest.Season;
            Rating = latest.Rating;
            CaptainGames = latest.CaptainGames;
            CaptainWins = latest.CaptainWins;
            Rating_Standard = latest.Rating_Standard;
            GamesPlayed = latest.GamesPlayed;
            GamesPlayed_Standard = latest.GamesPlayed_Standard;
            GamesPlayed_Custom = latest.GamesPlayed_Custom;
            GamesPlayed_Event = latest.GamesPlayed_Event;
            GamesPlayed_Pro = latest.GamesPlayed_Pro;
            GamesPlayed_Tournament = latest.GamesPlayed_Tournament;
            GamesPlayed_Premade3v3 = latest.GamesPlayed_Premade3v3;
            GamesWon = latest.GamesWon;
            GamesWon_Standard = latest.GamesWon_Standard;
            GamesWon_Custom = latest.GamesWon_Custom;
            GamesWon_Event = latest.GamesWon_Event;
            GamesWon_Pro = latest.GamesWon_Pro;
            GamesWon_Tournament = latest.GamesWon_Tournament;
            GamesWon_Premade3v3 = latest.GamesWon_Premade3v3;
        }
    }

    protected bool Equals(StatsSnapshot other) =>
        GamesPlayed == other.GamesPlayed &&
        GamesPlayed_Standard == other.GamesPlayed_Standard &&
        GamesPlayed_Custom == other.GamesPlayed_Custom &&
        GamesPlayed_Event == other.GamesPlayed_Event &&
        GamesPlayed_Pro == other.GamesPlayed_Pro &&
        GamesPlayed_Tournament == other.GamesPlayed_Tournament &&
        GamesPlayed_Premade3v3 == other.GamesPlayed_Premade3v3 &&
        GamesWon == other.GamesWon &&
        GamesWon_Standard == other.GamesWon_Standard &&
        GamesWon_Custom == other.GamesWon_Custom &&
        GamesWon_Event == other.GamesWon_Event &&
        GamesWon_Pro == other.GamesWon_Pro &&
        GamesWon_Tournament == other.GamesWon_Tournament &&
        GamesWon_Premade3v3 == other.GamesWon_Premade3v3 &&
        CaptainGames == other.CaptainGames &&
        CaptainWins == other.CaptainWins &&
        Math.Abs(Rating_Standard - other.Rating_Standard) < 0.000001 &&
        Math.Abs(Rating - other.Rating) < 0.000001;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((StatsSnapshot)obj);
    }

    public override int GetHashCode()
    {
        HashCode hashCode = new();
        // ReSharper disable NonReadonlyMemberInGetHashCode
        hashCode.Add(Id);
        hashCode.Add(League);
        hashCode.Add(GamesPlayed);
        hashCode.Add(GamesWon);
        hashCode.Add(GamesPlayed_Standard);
        hashCode.Add(GamesWon_Standard);
        hashCode.Add(GamesPlayed_Pro);
        hashCode.Add(GamesWon_Pro);
        hashCode.Add(GamesPlayed_Tournament);
        hashCode.Add(GamesWon_Tournament);
        hashCode.Add(GamesPlayed_Premade3v3);
        hashCode.Add(GamesWon_Premade3v3);
        hashCode.Add(GamesPlayed_Event);
        hashCode.Add(GamesWon_Event);
        hashCode.Add(GamesPlayed_Custom);
        hashCode.Add(GamesWon_Custom);
        hashCode.Add(CaptainGames);
        hashCode.Add(CaptainWins);
        hashCode.Add(Rating);
        hashCode.Add(Rating_Standard);
        // ReSharper restore NonReadonlyMemberInGetHashCode
        return hashCode.ToHashCode();
    }
}
