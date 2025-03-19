using BCL.Domain.Entities.Users;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;

namespace BCL.Discord.Utils;

//This is only used by admin commands in case the user has left the server
public static class UserUtils
{
    public static async Task<(User?, DiscordUser?)> GetUser(BaseContext context, IUserRepository userRepository, DiscordUser? discordUser = null, string? inGameName = null)
    {
        if (discordUser is null && inGameName is null)
        {
            await context.CreateResponseAsync("Please provide a DiscordUser or an InGameName.", true);
            return (null, discordUser);
        }

        User? user = discordUser is null
            ? userRepository.GetByIgn(inGameName!)
            : userRepository.GetByDiscordId(discordUser.Id);

        try
        {
            discordUser ??= user is not null
                ? context.Guild.Members[user.DiscordId]
                : discordUser;
        }
        catch (Exception e)
        {
            if (e is not NotFoundException)
                Console.WriteLine(e);
        }

        return (user, discordUser);
    }
}
