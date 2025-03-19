using BCL.Domain.Dtos;
using BCL.Domain.Entities.Matches;
using BCL.Domain.Entities.Queue;

namespace BCL.Core.Services.Args;

public class MatchStartedEventArgs(Team team1, Team team2, Match match, List<string> format, Map map) : EventArgs
{
    public Team Team1 { get; set; } = team1;
    public Team Team2 { get; set; } = team2;
    public Match Match { get; set; } = match;
    public Map Map { get; set; } = map;
    public List<string> Format { get; set; } = format;
}
