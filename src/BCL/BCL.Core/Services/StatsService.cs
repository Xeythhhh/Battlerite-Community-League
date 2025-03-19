using BCL.Core.Services.Abstract;
using BCL.Domain;
using BCL.Domain.Entities.Analytics;
using BCL.Domain.Entities.Matches;
using BCL.Domain.Entities.Queue;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

namespace BCL.Core.Services;
public class StatsService(
    IUserRepository userRepository,
    IChampionRepository championRepository,
    IMapRepository mapRepository,
    IMatchRepository matchRepository,
    IAnalyticsRepository analyticsRepository) : IStatsService
{
    public async Task StartupRoutine()
    {
        await SyncMaps();
        await SyncAnalytics();
    }

    public async Task SyncAnalytics()
    {
        Console.WriteLine("Validating migrationInfo...");
        MigrationInfo migrationInfo = analyticsRepository.GetMigrationInfo() ?? analyticsRepository.CreateMigrationInfo().Entity;
        Console.WriteLine($"""

                           Last Bot Version: `{migrationInfo.LastBotVersionUsed}`
                           Last Bot Version used for Dev Command Database Migration: `{migrationInfo.Version}`
                           """);

        migrationInfo.LastBotVersionUsed = CoreConfig.Version;
        DomainConfig.Season = migrationInfo.Seasons[^1].Label;
        analyticsRepository.SaveChanges();

        List<User> users = userRepository.GetAll().OrderByDescending(u => u.CurrentSeasonStats.LongestDraftTime)
            .Where(u => u.CurrentSeasonStats.TimedDrafts > 0).ToList();

        await SyncRegionDraftTime(users, Region.Eu);
        await SyncRegionDraftTime(users, Region.Na);
        await SyncRegionDraftTime(users, Region.Sa);
    }

    private async Task SyncRegionDraftTime(IReadOnlyCollection<User> users, Region region)
    {
        RegionStats stats = analyticsRepository.GetRegionStats(region)
                    ?? analyticsRepository.AddRegionStats(new RegionStats(region)).Entity;

        User? longest = users.FirstOrDefault(u => u.Server == region);
        User? shortest = users.LastOrDefault(u => u.Server == region);

        if (longest is not null)
        {
            stats.LongestUser = longest;
            stats.LongestTime = longest.CurrentSeasonStats.LongestDraftTime;
            stats.LongestLink = longest.CurrentSeasonStats.LongestDraftLink;
        }

        if (shortest is not null)
        {
            stats.ShortestUser = shortest;
            stats.ShortestTime = shortest.CurrentSeasonStats.ShortestDraftTime;
            stats.ShortestLink = shortest.CurrentSeasonStats.ShortestDraftLink;
        }

        await analyticsRepository.SaveChangesAsync();

        Console.WriteLine($"Synced region draft time stats for {region}.");
    }

    public async Task SyncMaps()
    {
        //TODO Map Stats
        List<Map> maps = mapRepository.GetAll().ToList();
        List<Match> matches = matchRepository.GetAllCompleted().ToList();
        foreach (Map? map in maps) map.GamesPlayed = matches.Count(m => m.MapId == map.Id);
        await mapRepository.SaveChangesAsync();
        Console.WriteLine($"Synced stats for {maps.Count} maps.");
    }

    public async Task UpdateStats(Match match)
    {
        if (match.League is League.Premade3V3)
        {
            //TODO
            Console.WriteLine("Team stats will reflect in user stats but are not bound to teams");
        }

        if (match.Outcome is MatchOutcome.Canceled or MatchOutcome.InProgress) return;

        Map? map = await mapRepository.GetByIdAsync(match.MapId);
        map!.GamesPlayed++;
        await mapRepository.SaveChangesAsync();

        List<Champion> champions = GetDistinctChampionsInMatch(match);
        foreach (Champion champion in champions) UpdateChampion(champion, match);
        await championRepository.SaveChangesAsync();

        List<User> users = match.PlayerInfos.Select(p => userRepository.GetById(p.Id)!).ToList();
        foreach (User? user in users) UpdateUser(user, match);
        await userRepository.SaveChangesAsync();
    }

    private static void UpdateUser(User user, Match match)
    {
        if (user.IsInPlacements && match.League is League.Pro) user.PlacementGamesRemaining--;
        if (user.IsInPlacements_Standard && match.League is League.Standard) user.PlacementGamesRemaining_Standard--;

        bool isCaptain = match.IsCaptain(user.DiscordId, out bool isTeam1);
        bool win =
            (isTeam1 && (match.Outcome == MatchOutcome.Team1))
            || (!isTeam1 && (match.Outcome == MatchOutcome.Team2));

        StatsSnapshot newSnapshot = new(user)
        {
            League = match.League,
            Rating = user.Rating,
            Rating_Standard = user.Rating_Standard,
            Eloshift = match.PlayerEloShifts.First(p => p.Item1 == user.Id).Item2,
            MatchId = match.Id
        };

        if (user.CurrentSeasonStats.HighestRating < user.Rating) user.CurrentSeasonStats.HighestRating = user.Rating;
        if (user.CurrentSeasonStats.LowestRating > user.Rating) user.CurrentSeasonStats.LowestRating = user.Rating;

        if (user.CurrentSeasonStats.HighestRating_Standard < user.Rating_Standard) user.CurrentSeasonStats.HighestRating_Standard = user.Rating_Standard;
        if (user.CurrentSeasonStats.LowestRating_Standard > user.Rating_Standard) user.CurrentSeasonStats.LowestRating_Standard = user.Rating_Standard;

        SetSnapshotValues(user, match, isTeam1, newSnapshot, isCaptain);

        user.CurrentSeasonStats.Snapshots.Add(newSnapshot);

        //session stuff todo refactor
        int eloShift = Convert.ToInt32(match.PlayerEloShifts.First(p => p.Item1 == user.Id).Item2);
        if ((DateTime.UtcNow - (user.LastPlayed ?? DateTime.MinValue)).TotalHours >= DomainConfig.Profile.SessionTimeout)
        {
            user.SessionWins = win ? 1 : 0;
            user.SessionGames = 1;
            switch (match.League)
            {
                case League.Standard:
                    user.SessionEloChange = eloShift;
                    user.SessionEloChange_Pro = 0;
                    break;

                case League.Pro:
                    user.SessionEloChange = 0;
                    user.SessionEloChange_Pro = eloShift;
                    break;
            }
        }
        else
        {
            if (win) user.SessionWins++;
            user.SessionGames++;

            switch (match.League)
            {
                case League.Standard:
                    user.SessionEloChange += eloShift;
                    break;

                case League.Pro:
                    user.SessionEloChange_Pro += eloShift;
                    break;
            }
        }
        user.LastPlayed = DateTime.UtcNow;
    }
    private static void SetSnapshotValues(User user, Match match, bool isTeam1, StatsSnapshot snapshot, bool isCaptain)
    {
        bool win = (isTeam1 && match.Outcome is MatchOutcome.Team1) ||
                  (!isTeam1 && match.Outcome is MatchOutcome.Team2);

        #region GamesPlayed

        snapshot.GamesPlayed++;

        System.Reflection.PropertyInfo? gamesPlayedLeague = snapshot.GetType().GetProperty($"GamesPlayed_{match.League}");
        gamesPlayedLeague?.GetSetMethod()?.Invoke(snapshot,
        [
            (int)(gamesPlayedLeague.GetValue(snapshot) ?? 0) + 1
        ]);

        #endregion

        #region GamesWon

        if (win)
        {
            snapshot.GamesWon++;
            System.Reflection.PropertyInfo? gamesWonLeague = snapshot.GetType().GetProperty($"GamesWon_{match.League}");
            gamesWonLeague?.GetSetMethod()?.Invoke(snapshot,
            [
                (int)(gamesWonLeague.GetValue(snapshot) ?? 0) + 1
            ]);
        }

        #endregion

        #region WinStreak

        user.WinStreak = win
            ? user.WinStreak > 0 ? user.WinStreak + 1 : 1
            : user.WinStreak < 0 ? user.WinStreak - 1 : -1;

        System.Reflection.PropertyInfo? winStreakLeague = user.GetType().GetProperty($"WinStreak_{match.League}");
        int currentStreak = (int)(winStreakLeague?.GetValue(user) ?? 0);
        winStreakLeague?.GetSetMethod()?.Invoke(user,
        [
            win
                ? currentStreak > 0 ? currentStreak + 1 : 1
                : currentStreak < 0 ? currentStreak - 1 : -1
        ]);

        #endregion

        #region Captain

        if (isCaptain)
        {
            snapshot.CaptainGames++;

            if (win) snapshot.CaptainWins++;
        }

        #endregion
    }

    private void UpdateChampion(Champion champion, Match match)
    {
        ChampionStatsSnapshot newSnapshot = new(champion.CurrentSeasonStats.LatestSnapshot);
        SetSnapshotValues(champion, match, newSnapshot);

        #region MatchCount

        List<Match> matches = matchRepository.GetAllCompleted(DomainConfig.Season).ToList();
        newSnapshot.MatchCount = matches.Count;
        newSnapshot.GetType().GetProperty($"MatchCount_{match.League}")?
            .GetSetMethod()?.Invoke(newSnapshot, [matches.Count(m => m.League == match.League)]);

        #endregion

        champion.CurrentSeasonStats.Snapshots.Add(newSnapshot);
    }
    // ReSharper disable SuggestBaseTypeForParameter
    public static void SetSnapshotValues(Champion champion, Match match, ChampionStatsSnapshot snapshot)
    // ReSharper restore SuggestBaseTypeForParameter
    {
        snapshot.League = match.League;

        int picked = true switch
        {
            //_ when match.Picked(Match._Team.Team1, champion) && match.Picked(Match._Team.Team2, champion) => 2,
            _ when match.Picked(Match._Team.Team1, champion) || match.Picked(Match._Team.Team2, champion) => 1,
            _ => 0
        };

        int banned = true switch
        {
            //_ when match.Banned(Match._Team.Team1, champion) && match.Banned(Match._Team.Team2, champion) => 2,
            _ when match.Banned(Match._Team.Team1, champion) || match.Banned(Match._Team.Team2, champion) => 1,
            _ => 0
        };

        #region GamesPlayed

        snapshot.GamesPlayed += picked;
        System.Reflection.PropertyInfo? gamesPlayedLeague = snapshot.GetType().GetProperty($"GamesPlayed_{match.League}");
        gamesPlayedLeague?.GetSetMethod()?.Invoke(snapshot,
        [
            (int)(gamesPlayedLeague.GetValue(snapshot) ?? 0) + picked
        ]);

        #endregion

        #region GamesWon

        if (match.Won(champion))
        {
            snapshot.GamesWon++;
            System.Reflection.PropertyInfo? gamesWonLeague = snapshot.GetType().GetProperty($"GamesWon_{match.League}");
            gamesWonLeague?.GetSetMethod()?.Invoke(snapshot,
            [
                (int)(gamesWonLeague.GetValue(snapshot) ?? 0) + 1
            ]);
        }

        #endregion

        #region Banned

        snapshot.Banned += banned;
        System.Reflection.PropertyInfo? gamesBannedLeague = snapshot.GetType().GetProperty($"Banned_{match.League}");
        gamesBannedLeague?.GetSetMethod()?.Invoke(snapshot,
        [
            (int)(gamesBannedLeague.GetValue(snapshot) ?? 0) + banned
        ]);

        #endregion
    }

    private List<Champion> GetDistinctChampionsInMatch(Match match) =>
        match.Draft.Steps.Where(ds => ds.Action is not DraftAction.MapBan or DraftAction.MapPick or DraftAction.Reserve)
            .Select(ds => new[] { ds.TokenId1, ds.TokenId2 })
            .SelectMany(x => x).Distinct().Where(c => c != Ulid.Empty && c != default)
            .Select(id => championRepository.GetById(id)!).ToList();
}
