using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace BCL.Discord.Extensions;

public static class DiscordInteractionExtensions
{
    public static async Task DeleteResponseAsync(this InteractionContext context, int seconds = 0)
        => await context.Interaction.DeleteOriginalResponseAsync(seconds);

    public static async Task DeleteOriginalResponseAsync(this DiscordInteraction interaction, int seconds = 0)
    {
        await Task.Delay(TimeSpan.FromSeconds(seconds));
        await interaction.DeleteOriginalResponseAsync();
    }
}
