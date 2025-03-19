using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;

using BCL.Domain.Entities.Abstract;
using BCL.Domain.Enums;

// ReSharper disable InconsistentNaming

namespace BCL.Domain.Entities.Queue;
public class Champion : TokenEntity
{
    public ChampionClass Class { get; set; }
    public ChampionRole Role { get; set; }
    public string? Restrictions { get; set; }
    [NotMapped] public bool Restricted => !string.IsNullOrWhiteSpace(Restrictions);

    public virtual List<ChampionStats> Stats { get; set; } = [];
    public ChampionStats? GetStats(string season) => Stats.SingleOrDefault(s => s.Season == season);
    [NotMapped]
    public ChampionStats CurrentSeasonStats
    {
        get
        {
            ChampionStats? value = Stats.FirstOrDefault(s => s.Season == DomainConfig.Season);
            if (value is null)
            {
                value = new ChampionStats
                {
                    Season = DomainConfig.Season,
                    Snapshots = []
                };

                Stats.Add(value);
            }
            return value;
        }
    }

    public string LatestMatch_Label { get; set; } = string.Empty;
    public Uri LatestMatch { get; set; } = new Uri("about:blank");
    [NotMapped] public bool HasLatestMatchLink => LatestMatch.OriginalString != "about:blank";

    public static Expression<Func<Champion, bool>> IsMelee = champion =>
        champion.Role == ChampionRole.Dps && champion.Class == ChampionClass.Melee;

    public static Expression<Func<Champion, bool>> IsRanged = champion =>
        champion.Role == ChampionRole.Dps && champion.Class == ChampionClass.Ranged;

    public static Expression<Func<Champion, bool>> IsSupport = champion =>
        champion.Role == ChampionRole.Healer;
}
