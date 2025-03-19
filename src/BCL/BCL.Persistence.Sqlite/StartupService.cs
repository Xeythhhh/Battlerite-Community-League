using System.Text.Json;

using BCL.Domain.Entities.Queue;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Converters;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;

namespace BCL.Persistence.Sqlite;

public static class StartupService
{
    public static void AddPersistenceSqlite(this IServiceCollection services)
    {
        services.AddSingleton<ValueConverter<Ulid, string>, UlidToStringConverter>();
        services.AddSingleton<ValueConverter<Ulid, byte[]>, UlidToBytesConverter>();
        services.AddDbContext<BclDbContext>(options =>
            options.UseSqlite(PersistenceSqliteConfig.SqliteDatabase.ConnectionString, sqliteOptions =>
                sqliteOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)));

        Type[] assemblyTypes = PersistenceSqliteAssembly.Value.GetTypes();
        SetupRepositories(assemblyTypes, services);
    }

    public static void MigrateSqliteDatabase(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();
        BclDbContext? dbContext = scope.ServiceProvider.GetService<BclDbContext>();
        if (dbContext!.Database.GetPendingMigrations().Any()) dbContext.Database.Migrate();
    }

    public static void UseSqliteDatabaseSeed(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();
        IChampionRepository championRepository = scope.ServiceProvider.GetService<IChampionRepository>()!;
        IMapRepository mapRepository = scope.ServiceProvider.GetService<IMapRepository>()!;
        List<ChampionDto>? champions = JsonSerializer.Deserialize<List<ChampionDto>>(
            File.ReadAllText("Champions.json"));
        if (champions is not null &&
            champions.Count != championRepository.GetAll().Count())
        {
            foreach (Champion? newChampion in champions.Select(champion => new Champion
            {
                Name = champion.Name,
                Role = Enum.Parse<ChampionRole>(champion.Role),
                Class = Enum.Parse<ChampionClass>(champion.Class),
                Restrictions = champion.Restrictions ?? string.Empty,
                Disabled = champion.Disabled,
            }).Where(newChampion => championRepository.Get(c => c.Name == newChampion.Name).FirstOrDefault() is null))
            {
                championRepository.Add(newChampion);
            }
        }

        championRepository.SaveChanges();

        List<MapDto>? maps = JsonSerializer.Deserialize<List<MapDto>>(
            File.ReadAllText("Maps.json"));
        if (maps is not null &&
            maps.Count * 2 != mapRepository.GetAll().Count())
        {
            foreach ((string name, int day, int night) in maps)
            {
                if (mapRepository.Get(m => m.Name.StartsWith(name)).FirstOrDefault() is null)
                {
                    mapRepository.Add(new Map
                    {
                        Name = $"{name} - Day",
                        Variant = MapVariant.Day,
                        Frequency = day,
                        Disabled = false,
                    });

                    mapRepository.Add(new Map
                    {
                        Name = $"{name} - Night",
                        Variant = MapVariant.Night,
                        Frequency = night,
                        Disabled = false
                    });
                }
            }
        }

        mapRepository.SaveChanges();
    }

    static void SetupRepositories(IEnumerable<Type> assemblyTypes, IServiceCollection services)
    {
        IEnumerable<Type> repositoryTypes = assemblyTypes
            .Where(x => x.Name.EndsWith("Repository") && !x.IsAbstract);

        foreach (Type? implementationType in repositoryTypes)
        {
            services.AddTransient(implementationType);
            foreach (Type serviceType in implementationType.GetInterfaces())
                services.AddTransient(serviceType, implementationType);
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    record ChampionDto(
        string Name,
        string Class,
        string Role,
        string? Restrictions = null,
        bool Disabled = false);

    // ReSharper disable once ClassNeverInstantiated.Local
    record MapDto(
        string Name,
        int Day,
        int Night);
}
