using System.ComponentModel.DataAnnotations.Schema;

using BCL.Domain.Entities.Abstract;
using BCL.Domain.Entities.Analytics;
using BCL.Domain.Enums;

// ReSharper disable InconsistentNaming
#pragma warning disable CS8618
namespace BCL.Domain.Entities.Users;

public class Stats : Entity
{
    public string Season { get; set; }
    public double HighestRating { get; set; } = 0;
    public double LowestRating { get; set; } = DomainConfig.DefaultRating;
    public double HighestRating_Standard { get; set; } = 0;
    public double LowestRating_Standard { get; set; }

    //Draft
    public TimeSpan LongestDraftTime { get; set; } = TimeSpan.MinValue;
    public TimeSpan ShortestDraftTime { get; set; } = TimeSpan.MaxValue;
    public TimeSpan AverageDraftTime { get; set; } = TimeSpan.Zero;
    public int TimedDrafts { get; set; } = 0;

    public virtual List<StatsSnapshot> Snapshots { get; set; } = [];
    public StatsSnapshot? LatestSnapshot => Snapshots.MaxBy(s => s.RecordedAt);

    public bool PlayedPro => LatestSnapshot?.GamesPlayed_Pro > 0;
    public bool PlayedStandard => LatestSnapshot?.GamesPlayed_Standard > 0;
    public bool PlayedTournament => LatestSnapshot?.GamesPlayed_Tournament > 0;
    public bool PlayedEvent => LatestSnapshot?.GamesPlayed_Event > 0;
    public bool PlayedCustom => LatestSnapshot?.GamesPlayed_Custom > 0;

    public Uri LongestDraftLink { get; set; } = new Uri("about:blank");
    [NotMapped] public bool HasLongestLink => LongestDraftLink.OriginalString != "about:blank";
    public Uri ShortestDraftLink { get; set; } = new Uri("about:blank");
    [NotMapped] public bool HasShortestLink => ShortestDraftLink.OriginalString != "about:blank";

    public string? Disclaimer { get; set; } //This is used for generated stats
    public League Membership { get; set; }
}
