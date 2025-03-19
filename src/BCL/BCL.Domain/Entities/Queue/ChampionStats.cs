using BCL.Domain.Entities.Abstract;
using BCL.Domain.Entities.Analytics;

namespace BCL.Domain.Entities.Queue;
public class ChampionStats : Entity, ICloneable
{
    public string Season { get; set; } = DomainConfig.Season;

    public virtual List<ChampionStatsSnapshot> Snapshots { get; set; } = [];
    public ChampionStatsSnapshot? LatestSnapshot => Snapshots.MaxBy(s => s.RecordedAt);

    public bool PlayedPro => LatestSnapshot?.GamesPlayed_Pro > 0;
    public bool PlayedStandard => LatestSnapshot?.GamesPlayed_Standard > 0;
    public bool PlayedTournament => LatestSnapshot?.GamesPlayed_Tournament > 0;
    public bool PlayedEvent => LatestSnapshot?.GamesPlayed_Event > 0;
    public bool PlayedCustom => LatestSnapshot?.GamesPlayed_Custom > 0;

    public ChampionStats Copy()
    {
        ChampionStats clone = (ChampionStats)Clone();
        clone.Id = Ulid.Empty;
        return clone;
    }

    public object Clone() => MemberwiseClone();
}
