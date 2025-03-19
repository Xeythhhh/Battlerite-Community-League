using BCL.Core.Services.Abstract;
using BCL.Domain.Dtos;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;

using DSharpPlus.Entities;

namespace BCL.Core.Services.Queue;
public class QueueService(IMatchmakingService matchmakingService) : IQueueService
{
    public static bool Enabled { get; set; }

    public static InhouseQueue Queue { get; } = new(CoreConfig.Queue.QueueSize);

    public static int EuProCount => Queue.EuProCount;
    public static int NaProCount => Queue.NaProCount;
    public static int SaProCount => Queue.SaProCount;
    public static int EuStandardCount => Queue.EuStandardCount;
    public static int NaStandardCount => Queue.NaStandardCount;
    public static int SaStandardCount => Queue.SaStandardCount;
    public static int EuPremade3V3Count => Queue.EuPremade3V3Count;
    public static int NaPremade3V3Count => Queue.NaPremade3V3Count;
    public static int SaPremade3V3Count => Queue.SaPremade3V3Count;

    public static bool TestMode { get; set; } = false;

    /// <summary>
    /// Add the user to the queue.
    /// </summary>
    public void Join(User user, QueueRole role = QueueRole.Fill, PremadeTeam? team = null)
    {
        if (!Enabled) return;

        if (team is not null)
        {
            // 🚀 **Batch Add Players**
            foreach (User member in team.Members)
            {
                Queue.Add(member, QueueRole.Fill, team);
            }
        }
        else
        {
            Queue.Add(user, role);
        }

        // 🚀 **Try to pop a match in the background**
        _ = Task.Run(async () =>
        {
            InhouseQueue.QueuePopResult queueResponse = await Queue.TryPop();
            if (queueResponse.Result)
            {
                await matchmakingService.CreateMatch(queueResponse.MatchDetails!);
            }
        });
    }

    /// <summary>
    /// Remove user from the queue.
    /// </summary>
    public void Leave(ulong discordId) => _Leave(discordId);

    public static void _Leave(ulong discordId)
    {
        // 🚀 **Efficient direct lookup**
        QueuePlayer? player = Queue.Players.Values.FirstOrDefault(p => p.DiscordId == discordId);
        if (player is null) return;

        Queue.Leave(player.Id);
    }

    /// <summary>
    /// Check if a user is in the queue.
    /// </summary>
    public bool IsUserInQueue(User user) => IsUserInQueue(user.DiscordId);
    public bool IsUserInQueue(DiscordUser discordUser) => IsUserInQueue(discordUser.Id);
    public bool IsUserInQueue(ulong discordId) => Queue.Players.Values.Any(player => player.DiscordId == discordId);

    /// <summary>
    /// Clears the queue.
    /// </summary>
    public List<ulong> Purge()
    {
        List<ulong> userDiscordIds = Queue.Players.Values.Select(player => player.DiscordId).ToList();
        Queue.Players.Clear();
        _ = Queue.DebouncedOnCollectionChanged(); // 🚀 **Non-blocking dashboard update**
        return userDiscordIds;
    }

    /// <summary>
    /// Get the current role of a user.
    /// </summary>
    public QueueRole CurrentRole(ulong discordId)
        => Queue.Players.Values.FirstOrDefault(u => u.DiscordId == discordId)?.Role ?? QueueRole.Fill;

    /// <summary>
    /// Check if a user is in a premade queue.
    /// </summary>
    public bool IsInPremadeQueue(DiscordUser user, out ulong[] discordIds)
        => IsInPremadeQueue(user.Id, out discordIds);

    private static bool IsInPremadeQueue(ulong userId, out ulong[] discordIds)
    {
        QueuePlayer? player = Queue.Players.Values.FirstOrDefault(p => p.DiscordId == userId && p.Premade);
        if (player is not null)
        {
            discordIds = Queue.Players.Values
                .Where(p => p.Premade && p.TeamId == player.TeamId)
                .Select(p => p.DiscordId)
                .ToArray();
            return true;
        }

        discordIds = [];
        return false;
    }

    public static string DisabledReason { get; set; } = "Bot just started";
}
