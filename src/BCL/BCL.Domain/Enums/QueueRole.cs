using DSharpPlus.SlashCommands;

namespace BCL.Domain.Enums;

public enum QueueRole
{
    [ChoiceName("Melee")] Melee,
    [ChoiceName("Ranged")] Ranged,
    [ChoiceName("Support")] Support,
    [ChoiceName("Fill")] Fill
}
