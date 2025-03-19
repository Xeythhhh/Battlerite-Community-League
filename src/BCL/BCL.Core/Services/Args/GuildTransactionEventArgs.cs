using BCL.Domain.Entities.Users;

namespace BCL.Core.Services.Args;
public class GuildTransactionEventArgs(User.BalanceSnapshot snapshot, ulong discordId) : EventArgs
{
    public User.BalanceSnapshot Snapshot { get; set; } = snapshot;
    public ulong DiscordId { get; set; } = discordId;
}
