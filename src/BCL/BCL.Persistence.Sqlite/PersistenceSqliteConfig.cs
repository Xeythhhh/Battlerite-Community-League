using Microsoft.Extensions.Configuration;

namespace BCL.Persistence.Sqlite;

#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8618
public static class PersistenceSqliteConfig
{
    static IConfiguration _configuration;

    public static void Setup(IConfiguration configuration) => _configuration = configuration;

    public static class SqliteDatabase
    {
        public static string ConnectionString => _configuration.GetConnectionString("SqliteConnectionString");
    }
}
