using System.Linq.Expressions;

using BCL.Domain.Entities.Analytics;
using BCL.Domain.Entities.Users;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;

namespace BCL.Persistence.Sqlite.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    protected readonly DbSet<Stats> Stats;
    protected readonly DbSet<StatsSnapshot> Snapshots;

    public UserRepository(BclDbContext dbContext) : base(dbContext)
    {
        Stats = DbContext.Set<Stats>();
        Snapshots = DbContext.Set<StatsSnapshot>();
    }

    public override IEnumerable<User> GetAll() => Users;

    public override User? GetById(Ulid id) => Users.SingleOrDefault(m => m.Id == id);
    public override User? GetById(string id) => Users.SingleOrDefault(m => m.Id == Ulid.Parse(id));
    public override async ValueTask<User?> GetByIdAsync(Ulid id) => await Users.SingleOrDefaultAsync(m => m.Id == id);
    public override async ValueTask<User?> GetByIdAsync(string id) => await Users.SingleOrDefaultAsync(m => m.Id == Ulid.Parse(id));

    // ReSharper disable once SpecifyStringComparison
    public User? GetByIgn(string ign) => Users.SingleOrDefault(m => m.InGameName == ign);
    public User? GetByDiscordUser(DiscordUser discordUser) => GetByDiscordId(discordUser.Id);
    public User? GetByDiscordId(ulong discordId) => Users.SingleOrDefault(m => m.DiscordId == discordId);
    public override IEnumerable<User> Get(Expression<Func<User, bool>> predicate) => Users.Where(predicate);

    public EntityEntry<StatsSnapshot> Delete(StatsSnapshot snapshot) => Snapshots.Remove(snapshot);
    public EntityEntry<Stats> Delete(Stats stats)
    {
        Snapshots.RemoveRange(stats.Snapshots);
        return Stats.Remove(stats);
    }

    public override int Count(Expression<Func<User, bool>> expression) => Users.Count(expression);

    private IIncludableQueryable<User, List<StatsSnapshot>> Users => DbSet.Include(u => u.SeasonStats).ThenInclude(s => s.Snapshots);
}
