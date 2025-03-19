using System.Linq.Expressions;

using BCL.Domain.Entities.Analytics;
using BCL.Domain.Entities.Queue;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;

namespace BCL.Persistence.Sqlite.Repositories;

public class ChampionRepository : GenericRepository<Champion>, IChampionRepository
{
    protected readonly DbSet<ChampionStatsSnapshot> Snapshots;
    protected readonly DbSet<ChampionStats> Stats;

    public ChampionRepository(BclDbContext dbContext) : base(dbContext)
    {
        Snapshots = DbContext.Set<ChampionStatsSnapshot>();
        Stats = DbContext.Set<ChampionStats>();
    }

    public override IEnumerable<Champion> GetAll() => Champions;
    public IEnumerable<Champion> GetAllEnabled() => Champions.Where(c => !c.Disabled).ToList();
    public IEnumerable<Champion> GetAllDisabled() => Champions.Where(c => c.Disabled).ToList();
    public IEnumerable<Champion> GetRestricted() => Champions.Where(c => c.Disabled || !string.IsNullOrWhiteSpace(c.Restrictions)).ToList();

    public override Champion? GetById(Ulid id) => Champions.SingleOrDefault(c => c.Id == id);
    public override Champion? GetById(string id) => Champions.SingleOrDefault(c => c.Id == Ulid.Parse(id));
    public override async ValueTask<Champion?> GetByIdAsync(Ulid id) => await Champions.SingleOrDefaultAsync(c => c.Id == id);
    public override async ValueTask<Champion?> GetByIdAsync(string id) => await Champions.SingleOrDefaultAsync(c => c.Id == Ulid.Parse(id));
    public override IEnumerable<Champion> Get(Expression<Func<Champion, bool>> predicate) => Champions.Where(predicate);

    public EntityEntry<ChampionStatsSnapshot> Delete(ChampionStatsSnapshot snapshot) => Snapshots.Remove(snapshot);
    public EntityEntry<ChampionStats> Delete(ChampionStats stats)
    {
        Snapshots.RemoveRange(stats.Snapshots);
        return Stats.Remove(stats);
    }

    public override int Count(Expression<Func<Champion, bool>> expression) => Champions.Count(expression);

    private IIncludableQueryable<Champion, List<ChampionStatsSnapshot>> Champions => DbSet.Include(u => u.Stats).ThenInclude(s => s.Snapshots);
}
