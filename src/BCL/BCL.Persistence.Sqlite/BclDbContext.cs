using BCL.Domain;
using BCL.Domain.Entities.Abstract;
using BCL.Domain.Entities.Analytics;
using BCL.Domain.Entities.Matches;
using BCL.Domain.Entities.Queue;
using BCL.Domain.Entities.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#pragma warning disable CS8618
namespace BCL.Persistence.Sqlite;

public sealed class BclDbContext(
    DbContextOptions<BclDbContext> options,
    ValueConverter<Ulid, string> ulidToStringConverter) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<PremadeTeam> Teams { get; set; }
    public DbSet<Champion> Champions { get; set; }
    public DbSet<Map> Maps { get; set; }
    public DbSet<Draft> Drafts { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<Stats> Stats { get; set; }
    public DbSet<ChampionStats> ChampionStats { get; set; }
    public DbSet<StatsSnapshot> StatsSnapshots { get; set; }
    public DbSet<ChampionStatsSnapshot> ChampionStatsSnapshots { get; set; }
    public DbSet<RegionStats> RegionDraftTimes { get; set; }
    public DbSet<MigrationInfo> MigrationInfo { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        //todo set up logger here
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        //TODO Create builders for each entity
        Type entityBaseType = typeof(Entity);
        IEnumerable<Type> entityTypes = DomainAssembly.Value.GetTypes()
            .Where(t => entityBaseType.IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false });

        foreach (Type? entityType in entityTypes)
        {
            builder.Entity(entityType)
                .Property("Id")
                .HasConversion(ulidToStringConverter);
        }

        builder.Entity<Match>()
            .Property(m => m.MapId)
            .HasConversion(ulidToStringConverter);
        builder.Entity<Match>()
            .Property(m => m.JumpLink)
            .HasConversion(v => v.ToString(), v => new Uri(v));

        builder.Entity<DraftStep>()
            .Property(ds => ds.TokenId1)
            .HasConversion(ulidToStringConverter);
        builder.Entity<DraftStep>()
            .Property(ds => ds.TokenId2)
            .HasConversion(ulidToStringConverter);

        builder.Entity<StatsSnapshot>()
            .Property(s => s.MatchId)
            .HasConversion(ulidToStringConverter);
        builder.Entity<ChampionStatsSnapshot>()
            .Property(s => s.MatchId)
            .HasConversion(ulidToStringConverter);

        builder.Entity<User>()
            .Property(u => u.TeamId)
            .HasConversion(ulidToStringConverter);
        builder.Entity<User>()
            .HasMany(e => e.SeasonStats);
        builder.Entity<User>()
            .Property(u => u.LatestMatchLink)
            .HasConversion(v => v.ToString(), v => new Uri(v));

        builder.Entity<Stats>()
            .HasMany(e => e.Snapshots);

        builder.Entity<Champion>()
            .HasMany(c => c.Stats);
        builder.Entity<ChampionStats>()
            .HasMany(e => e.Snapshots);

        builder.Entity<RegionStats>()
            .Property(rdt => rdt.Average)
            .HasConversion(new TimeSpanToTicksConverter());
        builder.Entity<RegionStats>()
            .Property(rdt => rdt.LongestTime)
            .HasConversion(new TimeSpanToTicksConverter());
        builder.Entity<RegionStats>()
            .Property(rdt => rdt.ShortestTime)
            .HasConversion(new TimeSpanToTicksConverter());
        builder.Entity<RegionStats>()
            .Property(e => e.LongestLink)
            .HasConversion(v => v.ToString(), v => new Uri(v));
        builder.Entity<RegionStats>()
            .Property(e => e.ShortestLink)
            .HasConversion(v => v.ToString(), v => new Uri(v));

        builder.Entity<Stats>()
            .Property(s => s.AverageDraftTime)
            .HasConversion(new TimeSpanToTicksConverter());
        builder.Entity<Stats>()
            .Property(s => s.LongestDraftTime)
            .HasConversion(new TimeSpanToTicksConverter());
        builder.Entity<Stats>()
            .Property(s => s.ShortestDraftTime)
            .HasConversion(new TimeSpanToTicksConverter());
        builder.Entity<Stats>()
            .Property(e => e.LongestDraftLink)
            .HasConversion(v => v.ToString(), v => new Uri(v));
        builder.Entity<Stats>()
            .Property(e => e.ShortestDraftLink)
            .HasConversion(v => v.ToString(), v => new Uri(v));

        builder.Entity<Champion>()
            .Property(e => e.LatestMatch)
            .HasConversion(v => v.ToString(), v => new Uri(v));
    }
    public override int SaveChanges()
    {
        AddTimestamps();
        return base.SaveChanges();
    }

    public async Task<int> SaveChangesAsync()
    {
        AddTimestamps();
        return await base.SaveChangesAsync();
    }

    void AddTimestamps()
    {
        IEnumerable<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry> entities = ChangeTracker.Entries()
            .Where(x => x is { Entity: Entity, State: EntityState.Added or EntityState.Modified });

        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry? entity in entities)
        {
            DateTime now = DateTime.UtcNow;

            if (entity.State == EntityState.Added) ((Entity)entity.Entity).CreatedAt = now;
            ((Entity)entity.Entity).LastUpdatedAt = now;
        }
    }
}
