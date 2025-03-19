using DSharpPlus.Entities;

namespace BCL.Discord.Extensions;
public static class DiscordMessageExtensions
{
    public static async Task DeleteAsync(this DiscordMessage message, int seconds = 0)
    {
        await Task.Delay(TimeSpan.FromSeconds(seconds));
        await message.DeleteAsync();
    }
}
