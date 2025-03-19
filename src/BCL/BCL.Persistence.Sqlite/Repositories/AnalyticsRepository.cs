using System.Diagnostics;

using BCL.Domain;
using BCL.Domain.Entities.Analytics;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BCL.Persistence.Sqlite.Repositories;
public class AnalyticsRepository : IAnalyticsRepository
{
    protected readonly BclDbContext DbContext;
    protected readonly DbSet<RegionStats> RegionDraftTimes;
    protected readonly DbSet<MigrationInfo> MigrationInfos;

    public AnalyticsRepository(BclDbContext dbContext)
    {
        DbContext = dbContext;
        RegionDraftTimes = DbContext.Set<RegionStats>();
        MigrationInfos = DbContext.Set<MigrationInfo>();
    }

    public IEnumerable<RegionStats> GetAllRegionStats() => RegionDraftTimes.ToList();
    public RegionStats? GetRegionStats(Region region, string? season = null)
    {
        season ??= DomainConfig.Season;
        return RegionDraftTimes.SingleOrDefault(e => e.Region == region && e.Season == season);
    }

    public EntityEntry<RegionStats> AddRegionStats(RegionStats entity) => RegionDraftTimes.Add(entity);
    public void DeleteRegionStats(Ulid id) => RegionDraftTimes.Remove(RegionDraftTimes.Find(id)!);

    public int SaveChanges() => DbContext.SaveChanges();
    public async Task SaveChangesAsync() => await DbContext.SaveChangesAsync();
    public MigrationInfo? GetMigrationInfo() => MigrationInfos.SingleOrDefault();
    public EntityEntry<MigrationInfo> CreateMigrationInfo()
    {
        try
        {
            MigrationInfo? migration = GetMigrationInfo();
            if (migration is null) return MigrationInfos.Add(new MigrationInfo());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        throw new UnreachableException();
    }

    public void Dispose() => DbContext.Dispose();
}
