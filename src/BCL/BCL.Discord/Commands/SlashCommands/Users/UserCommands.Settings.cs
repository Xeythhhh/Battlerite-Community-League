using BCL.Discord.Attributes.Permissions;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace BCL.Discord.Commands.SlashCommands.Users;
public partial class UserCommands
{
    public enum SettingsOption
    {
        [ChoiceName("Profile")] General,
        [ChoiceName("Queue")] Queue,
        [ChoiceName("Match History")] MatchHistory,
        [ChoiceName("Channel and Roles")] ChannelAndRole,
        [ChoiceName("Embed")] Embed,
        [ChoiceName("Chart")] Chart,
        //[ChoiceName("Currency")] Currency
    }

    [SlashCommand("Settings", "Update your settings")]
    public async Task Settings(InteractionContext context,
        [Option("Setting", "What are you trying to customize")] SettingsOption setting) =>
        await _Settings(context, setting);

    [SlashCommand_Staff]
    [SlashCommand("Settings_Staff", "Update someone's settings", false)]
    public async Task Settings_Staff(InteractionContext context,
        [Option("User", "Discord user.")] DiscordUser discordUser,
        [Option("Setting", "What are you trying to customize")] SettingsOption setting) =>
        await _Settings(context, setting, discordUser);

    public async Task _Settings(BaseContext context, SettingsOption setting, DiscordUser? discordUser = null)
    {
        discordUser ??= context.User;
        Domain.Entities.Users.User? user = userRepository.GetByDiscordUser(discordUser); if (user is null) { await SuggestRegistration(context, discordUser); return; }

        //await context.DeferAsync();

        switch (setting)
        {
            case SettingsOption.ChannelAndRole:
                if (!user.Vip && context.User.Id == user.DiscordId) { await context.CreateResponseAsync("Only available to bcl supporters.", true); return; }
                await context.CreateResponseAsync(InteractionResponseType.Modal, VipChannelModal(user));
                break;

            case SettingsOption.Embed:
                if (!user.Vip && context.User.Id == user.DiscordId) { await context.CreateResponseAsync("Only available to bcl supporters.", true); return; }
                await context.CreateResponseAsync(InteractionResponseType.Modal, EmbedSettingsModal(user));
                break;

            case SettingsOption.Chart:
                if (!user.Vip && context.User.Id == user.DiscordId) { await context.CreateResponseAsync("Only available to bcl supporters.", true); return; }
                await context.CreateResponseAsync(InteractionResponseType.Modal, ChartSettingsModal(user));
                break;

            //case SettingsOption.Currency:
            //    if (!user.Vip && context.User.Id == user.DiscordId) { await context.CreateResponseAsync("Only available to bcl supporters.", true); return; }
            //    await context.CreateResponseAsync(InteractionResponseType.Modal, CurrencySettings(user));
            //    break;

            case SettingsOption.General:
                await context.CreateResponseAsync(InteractionResponseType.Modal, ProfileSettingsModal(user));
                break;

            case SettingsOption.MatchHistory:
                await context.CreateResponseAsync(InteractionResponseType.Modal, MatchHistorySettingsModal(user));
                break;

            case SettingsOption.Queue:
                await context.CreateResponseAsync(InteractionResponseType.Modal, QueueSettingsModal(user));
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(setting), setting, null);
        }
    }
}
