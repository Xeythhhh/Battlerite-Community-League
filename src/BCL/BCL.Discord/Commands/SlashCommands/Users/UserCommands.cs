using BCL.Discord.Bot;
using BCL.Domain;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

#pragma warning disable CS4014

namespace BCL.Discord.Commands.SlashCommands.Users;
[SlashModuleLifespan(SlashModuleLifespan.Scoped)]
public partial class UserCommands(
    IUserRepository userRepository,
    IMatchRepository matchRepository,
    IMapRepository mapRepository,
    DiscordEngine discordEngine) : ApplicationCommandModule
{
    [SlashCommand("Register", "Register for BCL!")]
    public async Task Register(InteractionContext context,
        [MaximumLength(30)]
        [Option("InGameName", "Your main account")] string inGameName)
    {
        if (context.Channel.IsPrivate) { await context.CreateResponseAsync("Unavailable in DMs"); return; }
        Domain.Entities.Users.User? user = userRepository.GetByDiscordId(context.User.Id) ?? userRepository.GetByIgn(inGameName);
        if (user is not null)
        {
            await context.CreateResponseAsync($"""
                                               You are already registered {context.User.Mention}!
                                               Discord:{user.Mention} | InGameName:`{user.InGameName}`
                                               """);
            return;
        }

        discordEngine.Guild = await discordEngine.Client.GetGuildAsync(discordEngine.Guild.Id, true);
        await context.CreateResponseAsync(InteractionResponseType.Modal, RegistrationModal(inGameName));
    }

    public enum ProfileType
    {
        [ChoiceName("General")] General,
        [ChoiceName("Match History")] MatchHistory,
        //[ChoiceName("Wallet")] Wallet,
    }

    [SlashCommand("Profile", "Display a user profile.")]
    public async Task Profile(InteractionContext context,
        [Option("User", "Discord user.")] DiscordUser? discordUser = null,
        [Option("Type", "Type")] ProfileType profileType = ProfileType.General,
        [Option("Ephemeral", "Only visible to you if true")] bool ephemeral = false)
    {
        discordUser ??= context.User;
        Domain.Entities.Users.User? user = userRepository.GetByDiscordId(discordUser.Id); if (user is null) { await SuggestRegistration(context, discordUser); return; }

        await context.DeferAsync(ephemeral);
        switch (profileType)
        {
            case ProfileType.General: await _SendProfileMessage(context, discordUser, user, [DomainConfig.Season], discordEngine); break;
            case ProfileType.MatchHistory: await _MatchHistory(context, discordUser, user, DomainConfig.Season); break;
            //case ProfileType.Wallet: await _Wallet(context, user, discordUser, new[] { DomainConfig.Season }); break;

            default:
                throw new ArgumentOutOfRangeException(nameof(profileType), profileType, null);
        }

        await userRepository.SaveChangesAsync(); //TODO Needed to apply the hotfix for MatchHistory and profile, ideally removed after bug is fixed, likely cause by premade queue
    }

    [ContextMenu(ApplicationCommandType.UserContextMenu, "Profile")]
    public async Task Profile_ContextMenu(ContextMenuContext context)
    {
        Domain.Entities.Users.User? user = userRepository.GetByDiscordId(context.TargetUser.Id); if (user is null) { SuggestRegistration(context, context.TargetUser); return; }

        await context.DeferAsync();
        await _SendProfileMessage(context, context.TargetUser, user, [DomainConfig.Season], discordEngine);
    }
}
