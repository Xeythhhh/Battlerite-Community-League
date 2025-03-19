using DSharpPlus.SlashCommands;

namespace BCL.Domain.Enums;

public enum League
{
    [ChoiceName("Pro League")] Pro,
    [ChoiceName("Standard League")] Standard,
    [ChoiceName("Event")] Event,
    [ChoiceName("Tournament")] Tournament,
    [ChoiceName("Custom")] Custom,
    [ChoiceName("Premade 3v3")] Premade3V3,
}
