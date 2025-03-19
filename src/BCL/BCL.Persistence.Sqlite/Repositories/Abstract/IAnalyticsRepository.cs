using BCL.Domain.Entities.Analytics;
using BCL.Domain.Enums;

using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BCL.Persistence.Sqlite.Repositories.Abstract;
public interface IAnalyticsRepository : IDisposable
{
    IEnumerable<RegionStats> GetAllRegionStats();
    RegionStats? GetRegionStats(Region region, string? season = null);
    EntityEntry<RegionStats> AddRegionStats(RegionStats entity);
    void DeleteRegionStats(Ulid id);
    int SaveChanges();
    Task SaveChangesAsync();
    MigrationInfo? GetMigrationInfo();
    EntityEntry<MigrationInfo> CreateMigrationInfo();
}
