using BCL.Domain.Entities.Analytics;
using BCL.Domain.Entities.Users;

using DSharpPlus.Entities;

using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BCL.Persistence.Sqlite.Repositories.Abstract;

public interface IUserRepository : IGenericRepository<User>
{
    User? GetByIgn(string ign);
    User? GetByDiscordId(ulong discordId);
    User? GetByDiscordUser(DiscordUser discordUser);

    EntityEntry<StatsSnapshot> Delete(StatsSnapshot snapshot);
    EntityEntry<Stats> Delete(Stats stats);
}
