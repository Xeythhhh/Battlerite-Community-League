using System.Collections.Concurrent;

using BCL.Domain.Dtos;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;

namespace BCL.Core.Services.Queue;
public class InhouseQueue(int queueSize)
{
    public record MatchDetails(List<QueuePlayer> Players, League League, Region Server);

    private class ValidQueue(
        Func<QueuePlayer, bool> predicate,
        Region server,
        League league,
        int? size = null)
    {
        public int Size { get; } = size ?? CoreConfig.Queue.QueueSize;
        public Func<QueuePlayer, bool> Predicate { get; } = predicate;
        public Region Server { get; } = server;
        public League League { get; } = league;
    }

    private readonly List<ValidQueue> _queues =
    [
        new(p => p.Na && p.Pro && (p.CrossRegion || p.Server is Region.Na), Region.Na, League.Pro),
        new(p => p.Eu && p.Pro && (p.CrossRegion || p.Server is Region.Eu), Region.Eu, League.Pro),
        new(p => p.Sa && p.Pro && (p.CrossRegion || p.Server is Region.Sa), Region.Sa, League.Pro),

        new(p => p.Na && p.Standard, Region.Na, League.Standard),
        new(p => p.Eu && p.Standard, Region.Eu, League.Standard),
        new(p => p.Sa && p.Standard, Region.Sa, League.Standard),

        new(p => p.Na && p.Premade3V3, Region.Na, League.Premade3V3),
        new(p => p.Eu && p.Premade3V3, Region.Eu, League.Premade3V3),
        new(p => p.Sa && p.Premade3V3, Region.Sa, League.Premade3V3),
    ];

    private readonly SemaphoreSlim _gate = new(1, 1);
    public readonly ConcurrentDictionary<Ulid, QueuePlayer> Players = new();
    private readonly ConcurrentDictionary<ValidQueue, int> _queueCounts = new();

    public int QueueSize { get; } = queueSize;

    // ✅ **Precomputed counts**
    public int NaProCount => _queueCounts.GetValueOrDefault(_queues[0], 0);
    public int EuProCount => _queueCounts.GetValueOrDefault(_queues[1], 0);
    public int SaProCount => _queueCounts.GetValueOrDefault(_queues[2], 0);
    public int NaStandardCount => _queueCounts.GetValueOrDefault(_queues[3], 0);
    public int EuStandardCount => _queueCounts.GetValueOrDefault(_queues[4], 0);
    public int SaStandardCount => _queueCounts.GetValueOrDefault(_queues[5], 0);
    public int NaPremade3V3Count => _queueCounts.GetValueOrDefault(_queues[6], 0) / 3;
    public int EuPremade3V3Count => _queueCounts.GetValueOrDefault(_queues[7], 0) / 3;
    public int SaPremade3V3Count => _queueCounts.GetValueOrDefault(_queues[8], 0) / 3;

    public void Add(User user, QueueRole role, PremadeTeam? team = null)
    {
        QueuePlayer player = new(user, role, team);
        if (!Players.TryAdd(user.Id, player)) return;

        foreach (ValidQueue queue in _queues)
        {
            if (queue.Predicate(player))
                _queueCounts.AddOrUpdate(queue, 1, (_, count) => count + 1);
        }

        // 🚀 **Triggers in the background**
        _ = DebouncedOnCollectionChanged();
    }

    public record QueuePopResult(bool Result, MatchDetails? MatchDetails);

    public async Task<QueuePopResult> TryPop()
    {
        // 🚀 **Avoid locking unless a match is possible**
        if (!_queues.Any(queue => _queueCounts.GetValueOrDefault(queue, 0) >= queue.Size))
            return new QueuePopResult(false, null);

        await _gate.WaitAsync(10000);

        try
        {
            foreach (ValidQueue queue in _queues)
            {
                if (_queueCounts.GetValueOrDefault(queue, 0) < queue.Size)
                    continue;

                List<QueuePlayer> matchedPlayers = new(queue.Size);
                List<Ulid> toRemove = new(queue.Size);

                foreach (QueuePlayer player in Players.Values)
                {
                    if (!queue.Predicate(player)) continue;
                    matchedPlayers.Add(player);
                    toRemove.Add(player.Id);
                    if (matchedPlayers.Count >= queue.Size) break;
                }

                if (matchedPlayers.Count < queue.Size)
                    continue;

                foreach (Ulid id in toRemove)
                {
                    Players.TryRemove(id, out _);
                }

                // refresh queue counts if match pops
                foreach (ValidQueue queueToUpdate in _queues)
                {
                    _queueCounts[queueToUpdate] = Players.Count(p => queueToUpdate.Predicate(p.Value));
                }

                // check if matched players are queueing multiple regions and leagues I guess

                // 🚀 **Trigger update in the background**
                _ = DebouncedOnCollectionChanged();

                return new QueuePopResult(true, new MatchDetails(matchedPlayers, queue.League, queue.Server));
            }
        }
        finally
        {
            _gate.Release();
        }

        return new QueuePopResult(false, null);
    }

    public event EventHandler? CollectionChanged;

    private CancellationTokenSource _collectionChangedToken = new();

    public async Task DebouncedOnCollectionChanged()
    {
        try
        {
            // 🚀 **Cancel previous task**
            _collectionChangedToken.Cancel();
            _collectionChangedToken.Dispose();

            // 🚀 **Create a fresh CancellationTokenSource**
            _collectionChangedToken = new CancellationTokenSource();

            // 🚀 **Wait 2s, but allow cancellation**
            await Task.Delay(3000, _collectionChangedToken.Token);

            // 🚀 **Only invoke if we weren't canceled**
            CollectionChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (TaskCanceledException)
        {
            // ✅ **Ignore canceled updates**
        }
    }

    public void Leave(Ulid userId)
    {
        if (!Players.TryRemove(userId, out QueuePlayer? player)) return;

        foreach (ValidQueue queue in _queues)
        {
            if (queue.Predicate(player))
                _queueCounts.AddOrUpdate(queue, 0, (_, count) => Math.Max(0, count - 1));
        }

        // 🚀 **Trigger update in the background**
        _ = DebouncedOnCollectionChanged();
    }
}
