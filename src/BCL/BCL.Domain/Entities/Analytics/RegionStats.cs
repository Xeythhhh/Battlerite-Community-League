using System.ComponentModel.DataAnnotations.Schema;

using BCL.Domain.Entities.Abstract;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;

namespace BCL.Domain.Entities.Analytics;
public class RegionStats(Region region) : Entity
{
    public Region Region { get; set; } = region;
    public string Season { get; set; } = DomainConfig.Season;

    //Draft
    public int TimedDrafts { get; set; } = 0;
    public TimeSpan LongestTime { get; set; } = TimeSpan.MinValue;
    public User? LongestUser { get; set; }
    public TimeSpan ShortestTime { get; set; } = TimeSpan.MaxValue;
    public User? ShortestUser { get; set; }
    public TimeSpan Average { get; set; } = TimeSpan.Zero;
    public Uri LongestLink { get; set; } = new Uri("about:blank");
    [NotMapped] public bool HasLongestLink => LongestLink.OriginalString != "about:blank";
    public Uri ShortestLink { get; set; } = new Uri("about:blank");
    [NotMapped] public bool HasShortestLink => ShortestLink.OriginalString != "about:blank";
}
