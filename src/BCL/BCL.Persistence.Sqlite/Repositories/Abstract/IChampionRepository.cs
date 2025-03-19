using BCL.Domain.Entities.Analytics;
using BCL.Domain.Entities.Queue;

using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BCL.Persistence.Sqlite.Repositories.Abstract;

public interface IChampionRepository : IGenericRepository<Champion>
{
    IEnumerable<Champion> GetAllEnabled();
    IEnumerable<Champion> GetAllDisabled();
    IEnumerable<Champion> GetRestricted();
    EntityEntry<ChampionStatsSnapshot> Delete(ChampionStatsSnapshot snapshot);
    EntityEntry<ChampionStats> Delete(ChampionStats stats);
}
