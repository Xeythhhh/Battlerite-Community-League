using BCL.Core.Services.Queue;
using BCL.Domain.Dtos;
using BCL.Domain.Entities.Matches;
using BCL.Domain.Entities.Queue;
using BCL.Domain.Enums;

namespace BCL.Core.Services.Abstract;

public interface IMatchmakingService
{
    Task<Match> CreateMatch(
        List<Player> players,
        Region server,
        League league,
        List<string>? format = null,
        MatchmakingLogic? logic = null,
        Map? map = null,
        string? team1Name = null,
        string? team2Name = null,
        DraftType? draftType = null);

    Task CreateMatch(InhouseQueue.MatchDetails matchDetails);
}
