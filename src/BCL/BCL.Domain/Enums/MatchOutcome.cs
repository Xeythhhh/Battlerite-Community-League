using DSharpPlus.SlashCommands;

namespace BCL.Domain.Enums;

public enum MatchOutcome
{
    [ChoiceName("Team 1 Win")] Team1,
    [ChoiceName("Team 2 Win")] Team2,
    [ChoiceName("Drop")] Canceled,
    InProgress
}
