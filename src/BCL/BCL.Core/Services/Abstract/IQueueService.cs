using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;

using DSharpPlus.Entities;

namespace BCL.Core.Services.Abstract;
public interface IQueueService
{
    void Join(User user, QueueRole role = QueueRole.Fill, PremadeTeam? team = null);
    QueueRole CurrentRole(ulong discordId);
    void Leave(ulong discordId);
    bool IsUserInQueue(ulong discordId);
    bool IsUserInQueue(User user);
    bool IsUserInQueue(DiscordUser discordUser);
    List<ulong> Purge();
    bool IsInPremadeQueue(DiscordUser user, out ulong[] discordIds);
}
