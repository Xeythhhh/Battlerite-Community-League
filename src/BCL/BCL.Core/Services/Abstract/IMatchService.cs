using BCL.Core.Services.Args;
using BCL.Domain.Dtos;
using BCL.Domain.Entities.Matches;
using BCL.Domain.Entities.Queue;
using BCL.Domain.Enums;

using DSharpPlus.Entities;

namespace BCL.Core.Services.Abstract;
public interface IMatchService
{
    delegate Task MatchStartedEvent(object sender, MatchStartedEventArgs args);
    delegate Task MatchFinishedEvent();
    void StartMatch(
        Team team1,
        Team team2,
        Match match,
        Map map,
        List<string> format,
        DraftType draftType);
    Task<double> FinishMatch(Ulid id);
    Task<double> FinishMatch(string id);
    void UpdateDraftTimeAndLink(Ulid matchId, List<DraftTime> draftTimes, DiscordMessage discordMessage);
}
