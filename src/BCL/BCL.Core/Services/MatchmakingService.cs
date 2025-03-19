using BCL.Core.Services.Abstract;
using BCL.Core.Services.Queue;
using BCL.Domain;
using BCL.Domain.Dtos;
using BCL.Domain.Entities.Matches;
using BCL.Domain.Entities.Queue;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using Combinatorics.Collections;

namespace BCL.Core.Services;

public class MatchmakingService(
    IMatchService matchService,
    IMapRepository mapRepository,
    IUserRepository userRepository,
    ITeamRepository teamRepository) : IMatchmakingService
{
    private static readonly SemaphoreSlim Gate;

    public static MatchmakingLogic ProLogic { get; set; } = MatchmakingLogic.Mmr;
    public static MatchmakingLogic StandardLogic { get; set; } = MatchmakingLogic.Mmr;
    public static DraftType ProDraftType { get; set; } = DraftType.Sequential;
    public static DraftType StandardDraftType { get; set; } = DraftType.Sequential;

    static MatchmakingService()
    {
        Gate = new SemaphoreSlim(1, 1);
    }

    public async Task CreateMatch(InhouseQueue.MatchDetails matchDetails)
    {
        await Gate.WaitAsync(10000);
        List<Player> players = matchDetails.Players
            .ConvertAll(player => new Player(userRepository.GetById(player.Id)!, player.Role))
;
        Gate.Release();

        await CreateMatch(players, matchDetails.Server, matchDetails.League);
    }

    public async Task<Match> CreateMatch( //this is so ugly TODO change after tourney EDIT: nah maybe later
        List<Player> players,
        Region server,
        League league,
        List<string>? format = null,
        MatchmakingLogic? logic = null,
        Map? map = null,
        string? team1Name = null,
        string? team2Name = null,
        DraftType? draftType = null)
    {
        logic ??= GetLogic(league);
        map ??= await GetRandomMapAsync(league);
        format ??= GetFormat(league);
        draftType ??= GetDraftType(league);
        (Team team1, Team team2) = CreateTeams(players, logic.Value, league, team1Name, team2Name);

        Match match = new()
        {
            BotVersion = CoreConfig.Version,
            Team1 = string.Join(" ", team1.Players.Select(p => $"{p.User.Id}|{p.User.DiscordId}|{p.Role}")),
            Team2 = string.Join(" ", team2.Players.Select(p => $"{p.User.Id}|{p.User.DiscordId}|{p.Role}")),
            MapId = map.Id,
            Season = DomainConfig.Season,
            Region = server,
            MatchmakingLogic = logic.Value,
            League = league,
            _discordUserIds = string.Join(" ",
                team1.Users.Select(u => u.DiscordId)
                    .Union(team2.Users.Select(u => u.DiscordId)))
        };

        AddEloModifiers(team1, team2, match);

        matchService.StartMatch(team1, team2, match, map, format, draftType.Value);

        return match;
    }

    private static void AddEloModifiers(Team team1, Team team2, Match match)
    {
        CheckHighSkillGap();

        if (team1.Players.All(p => p.User.IsInPlacements_Standard)
            && team2.Players.All(p => p.User.IsInPlacements_Standard))
        {
            return;
        }

        CheckPlacements();

        if (match.League is not League.Standard) return;
        if (Random.Shared.Next() % 7 == 0) match.EloModifiers.Add(Match.EloModifier.DoubleUp);

        void CheckHighSkillGap()
        {
            if (team1.Players.Any(player => team1.Players.Any(teammate =>
            {
                double rating = match.League is League.Pro ? player.User.Rating : player.User.Rating_Standard;
                double teammateRating = match.League is League.Pro ? teammate.User.Rating : teammate.User.Rating_Standard;
                double threshold = match.League is League.Pro ? 300 : 500;

                return rating - teammateRating >= threshold;
            })))
            {
                match.EloModifiers.Add(Match.EloModifier.HighSkillGap(match.League is League.Pro ? 0.9 : 0.75, Match.Side.Team1));
            }

            if (team2.Players.Any(player => team2.Players.Any(teammate =>
            {
                double rating = match.League is League.Pro ? player.User.Rating : player.User.Rating_Standard;
                double teammateRating = match.League is League.Pro ? teammate.User.Rating : teammate.User.Rating_Standard;
                double threshold = match.League is League.Pro ? 300 : 500;

                return rating - teammateRating >= threshold;
            })))
            {
                Match.EloModifier? mod = match.EloModifiers.FirstOrDefault(e => e.Label == Match.EloModifier.HighSkillGapLabel);
                if (mod is not null)
                    mod.AppliesTo = Match.Side.Both;
                else
                    match.EloModifiers.Add(Match.EloModifier.HighSkillGap(match.League is League.Pro ? 0.9 : 0.75, Match.Side.Team2));
            }
        }

        void CheckPlacements()
        {
            if (team1.Players.Any(player => player.User.IsInPlacements_Standard))
                match.EloModifiers.Add(Match.EloModifier.TeammateInPlacements(Match.Side.Team1));

            if (!team2.Players.Any(player => player.User.IsInPlacements_Standard))
                return;

            Match.EloModifier? mod = match.EloModifiers.FirstOrDefault(e => e.Label == Match.EloModifier.PlacementsLabel);
            if (mod is not null)
                mod.AppliesTo = Match.Side.Both;
            else
                match.EloModifiers.Add(Match.EloModifier.TeammateInPlacements(Match.Side.Team2));
        }
    }

    private static DraftType GetDraftType(League league) => league switch
    {
        League.Pro => ProDraftType,
        League.Standard => StandardDraftType,
        _ => ProDraftType
    };

    private static List<string> GetFormat(League league) => league switch
    {
        League.Pro => CoreConfig.Draft.ProFormat,
        League.Standard => CoreConfig.Draft.StandardFormat,
        League.Event => CoreConfig.Draft.ProFormat,
        League.Tournament => CoreConfig.Draft.ProFormat,
        League.Custom => CoreConfig.Draft.ProFormat,
        League.Premade3V3 => CoreConfig.Draft.ProFormat,
        _ => throw new ArgumentOutOfRangeException(nameof(league), league, null)
    };

    private static MatchmakingLogic GetLogic(League league) => league switch
    {
        League.Pro => ProLogic,
        League.Standard => StandardLogic,
        League.Premade3V3 => MatchmakingLogic.Premade,
        League.Event or League.Tournament or League.Custom => MatchmakingLogic.None,
        _ => throw new ArgumentOutOfRangeException(nameof(league), league, null)
    };

    async Task<Map> GetRandomMapAsync(League league)
    {
        await Gate.WaitAsync(10000); //avoid multiple calls from different threads to the same dbcontext during multiple match creation

        List<Map> maps = league == League.Pro || league == League.Tournament

            ? [.. mapRepository.GetAllEnabled().Where(m => m.Pro).OrderBy(m => m.Frequency)]

            : [.. mapRepository.GetAllEnabled().OrderBy(m => m.Frequency)];

        maps.Remove(maps.OrderByDescending(m => m.LastUpdatedAt).First()); //remove last played map for some variety

        int totalFrequency = maps.Sum(m => m.Frequency);
        int random = Random.Shared.Next(totalFrequency);
        Map map = maps.First(m => (random -= m.Frequency) < 0);
        Gate.Release();
        return map;
    }

    private (Team, Team) CreateTeams(
        List<Player> players,
        MatchmakingLogic logic,
        League league,
        string? team1Name,
        string? team2Name) =>
        logic switch
        {
            MatchmakingLogic.Mmr => GetMmrTeams(players, league),
            MatchmakingLogic.Random => GetRandomTeams(players, league),
            MatchmakingLogic.Premade => GetPremadeTeams(players),
            MatchmakingLogic.None => GetCustomTeams(players, team1Name, team2Name),
            _ => throw new ArgumentOutOfRangeException(nameof(logic), logic, "Invalid Matchmaking Logic")
        };

    private (Team, Team) GetPremadeTeams(IReadOnlyCollection<Player> players)
    {
        List<PremadeTeam?> teams = [.. players.Select(p => p.User.TeamId).Distinct()
            .Select(teamRepository.GetById)
            .OrderByDescending(t => t!.Rating)];

        PremadeTeam? team1;
        PremadeTeam? team2;
        if (Random.Shared.Next() % 2 == 0)
        {
            team1 = teams[0];
            team2 = teams[^1];
        }
        else
        {
            team1 = teams[^1];
            team2 = teams[0];
        }

        return (
            new Team(team1!.Members.ConvertAll(m => players.First(p => p.User.Id == m.Id)),
                true,
                team1.Name,
                team1.Rating),

            new Team(team2!.Members.ConvertAll(m => players.First(p => p.User.Id == m.Id)),
                false,
                team2.Name,
                team2.Rating));
    }

    private static (Team, Team) GetCustomTeams(
        List<Player> players,
        string? team1Name,
        string? team2Name)
    {
        int teamSize = players.Count / 2;
        List<Player> team1 = players.Take(teamSize).ToList();
        List<Player> team2 = players.TakeLast(teamSize).ToList();

        return (new Team(team1, true, team1Name), new Team(team2, false, team2Name));
    }

    static (Team, Team) GetMmrTeams(IEnumerable<Player> players, League league)
    {
        MatchConfig bestConfig = new(int.MaxValue);
        Permutations<Player> permutations = new(players);
        foreach (IReadOnlyList<Player> permutation in permutations)
        {
            List<Player> playerList = [.. permutation];

            List<Player> team1 = playerList.Take(3).ToList();
            List<Player> team2 = playerList.TakeLast(3).ToList();
            double diff = league switch
            {
                League.Pro => Math.Abs(team1.Average(p => p.User.Rating) - team2.Average(p => p.User.Rating)),
                League.Standard => Math.Abs(team1.Average(p => p.User.Rating_Standard) - team2.Average(p => p.User.Rating_Standard)),
                League.Event => int.MaxValue,
                League.Tournament => int.MaxValue,
                League.Custom => int.MaxValue,
                _ => throw new ArgumentOutOfRangeException(nameof(league), league, null)
            };

            if (diff < bestConfig.Difference)
            {
                bestConfig = new MatchConfig(diff)
                {
                    Team1 = league == League.Pro
                        ? [.. team1.OrderByDescending(p => p.User.Rating)]
                        : [.. team1.OrderBy(_ => Random.Shared.Next())],

                    Team2 = league == League.Pro
                        ? [.. team2.OrderByDescending(p => p.User.Rating)]
                        : [.. team2.OrderBy(_ => Random.Shared.Next())],
                };
            }
        }

        bool team1FirstPick =
            (bestConfig.Team1.Average(p =>
                 league is League.Pro
                     ? p.User.Rating
                     : p.User.Rating_Standard)

             >

             bestConfig.Team2.Average(p =>
                 league is League.Pro
                     ? p.User.Rating
                     : p.User.Rating_Standard));

        if (league is League.Standard) team1FirstPick = !team1FirstPick;

        return team1FirstPick
            ? (new Team(bestConfig.Team1, true), new Team(bestConfig.Team2))
            : (new Team(bestConfig.Team2, true), new Team(bestConfig.Team1));
    }

    static (Team, Team) GetRandomTeams(List<Player> players, League league)
    {
        if (players.Count % 2 != 0) throw new ArgumentException("Can not have odd number of players in matchmaking");

        List<Player> team1 = [];
        List<Player> team2 = [];

        while (players.Count > 0)
        {
            Player p1 = players[Random.Shared.Next(players.Count - 1)];
            team1.Add(p1);
            players.Remove(p1);

            Player p2 = players[Random.Shared.Next(players.Count - 1)];
            team2.Add(p2);
            players.Remove(p2);
        }

        bool team1FirstPick =
            (team1.Average(p =>
                 league is League.Pro
                     ? p.User.Rating
                     : p.User.Rating_Standard)

             >

             team2.Average(p =>
                 league is League.Pro
                     ? p.User.Rating
                     : p.User.Rating_Standard));

        return team1FirstPick
            ? (new Team(team1, true), new Team(team2))
            : (new Team(team2, true), new Team(team1));
    }
}

internal class MatchConfig(double difference)
{
    public double Difference { get; set; } = difference;
    public List<Player> Team1 { get; set; } = [];
    public List<Player> Team2 { get; set; } = [];
}
