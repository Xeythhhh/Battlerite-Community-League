using BCL.Domain.Entities.Users;

using DSharpPlus.Entities;

namespace BCL.Persistence.Sqlite.Repositories.Abstract;

public interface ITeamRepository : IGenericRepository<PremadeTeam>
{
    public IEnumerable<PremadeTeam> GetByDiscordUser(DiscordUser discordUser);
    public IEnumerable<PremadeTeam> GetByDiscordUserId(ulong discordUserId);
}
