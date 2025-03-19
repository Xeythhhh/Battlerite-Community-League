using BCL.Core.Services.Abstract;
using BCL.Discord.Bot;
using BCL.Discord.Commands.SlashCommands;
using BCL.Discord.Commands.SlashCommands.Users;
using BCL.Discord.Extensions;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus;
using DSharpPlus.ButtonCommands;
using DSharpPlus.Entities;

#pragma warning disable CS4014

namespace BCL.Discord.Commands.ButtonCommands;
public class UserSettingsButtons(
    IUserRepository userRepository,
    IQueueService queueService,
    DiscordEngine discordEngine) : ButtonCommandModule
{
    public enum QueueSetting
    {
        Eu, Na,
        Standard, Pro,
        Sa
    }

    [ButtonCommand("ToggleQueueSetting")]
    public async Task ToggleQueueSetting(ButtonContext context, QueueSetting setting)
    {
        await context.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .WithContent($"Handling **Settings Interaction** for {context.User.Mention}...")
                .AsEphemeral());

        User? user = userRepository.GetByDiscordUser(context.User); if (user is null) { await UserCommands.SuggestRegistration(context, true); return; }

        if (queueService.IsInPremadeQueue(context.User, out _))
        {
            await context.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Can not change settings while in premade queue."));
            return;
        }

        string content;
        bool euUpdated = false;
        bool naUpdated = false;
        bool saUpdated = false;
        bool standardUpdated = false;
        bool proUpdated = false;

        switch (setting)
        {
            case QueueSetting.Eu:
                user.Eu = !user.Eu;
                content = $"**{QueueSetting.Eu}** queue set to `{user.Eu}` for {user.Mention}";
                euUpdated = true;
                break;

            case QueueSetting.Na:
                user.Na = !user.Na;
                content = $"**{QueueSetting.Na}** queue set to `{user.Na}` for {user.Mention}";
                naUpdated = true;
                break;

            case QueueSetting.Sa:
                user.Sa = !user.Sa;
                content = $"**{QueueSetting.Sa}** queue set to `{user.Sa}` for {user.Mention}";
                saUpdated = true;
                break;

            case QueueSetting.Standard:
                user.StandardQueue = !user.StandardQueue;
                content = $"**{QueueSetting.Standard}** queue set to `{user.StandardQueue}` for {user.Mention}";
                standardUpdated = true;
                break;

            case QueueSetting.Pro:
                user.ProQueue = user is { Pro: true, ProQueue: false };
                content = $"**{QueueSetting.Pro}** queue set to `{user.ProQueue}` for {user.Mention}";
                proUpdated = true;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(setting), setting, null);
        }

        UpdateQueueRoles(context, user, euUpdated, naUpdated, saUpdated, proUpdated, standardUpdated);

        if (queueService.IsUserInQueue(context.User.Id)) UpdateQueue(context, user);

        await userRepository.SaveChangesAsync();

        await context.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
            .WithContent(content)
            .AddMention(new UserMention(user.DiscordId)));

        context.Interaction.DeleteOriginalResponseAsync(5);
    }

    private static void UpdateQueueRoles(
        ButtonContext context,
        User user,
        bool euUpdated,
        bool naUpdated,
        bool saUpdated,
        bool proUpdated,
        bool standardUpdated)
    {
        IReadOnlyDictionary<ulong, DiscordRole> guildRoles = context.Guild.Roles;
        if (euUpdated)
        {
            switch (user.Eu)
            {
                case true: context.Member.GrantRoleAsync(guildRoles[DiscordConfig.Roles.QueuesEuId]); break;
                case false: context.Member.RevokeRoleAsync(guildRoles[DiscordConfig.Roles.QueuesEuId]); break;
            }
        }

        if (naUpdated)
        {
            switch (user.Na)
            {
                case true: context.Member.GrantRoleAsync(guildRoles[DiscordConfig.Roles.QueuesNaId]); break;
                case false: context.Member.RevokeRoleAsync(guildRoles[DiscordConfig.Roles.QueuesNaId]); break;
            }
        }

        if (saUpdated)
        {
            switch (user.Sa)
            {
                case true: context.Member.GrantRoleAsync(guildRoles[DiscordConfig.Roles.QueuesSaId]); break;
                case false: context.Member.RevokeRoleAsync(guildRoles[DiscordConfig.Roles.QueuesSaId]); break;
            }
        }

        if (proUpdated)
        {
            switch (user.ProQueue)
            {
                case true: context.Member.GrantRoleAsync(guildRoles[DiscordConfig.Roles.QueuesProId]); break;
                case false: context.Member.RevokeRoleAsync(guildRoles[DiscordConfig.Roles.QueuesProId]); break;
            }
        }

        // ReSharper disable once InvertIf
        if (standardUpdated)
        {
            switch (user.StandardQueue)
            {
                case true: context.Member.GrantRoleAsync(guildRoles[DiscordConfig.Roles.QueuesStandardId]); break;
                case false: context.Member.RevokeRoleAsync(guildRoles[DiscordConfig.Roles.QueuesStandardId]); break;
            }
        }
    }

    private void UpdateQueue(ButtonContext context, User user)
    {
        QueueRole role = queueService.CurrentRole(context.User.Id);
        queueService.Leave(context.User.Id);
        QueueCommands.ClearPurgeJob(context.User.Id);

        if ((user.Na || user.Eu) &&
            (user.StandardQueue || user.ProQueue))
        {
            QueueCommands._JoinQueue(context.Interaction, context.Client, userRepository, queueService, discordEngine, role, false);
        }
        else
        {
            context.Member.RevokeRoleAsync(context.Guild.Roles[DiscordConfig.Roles.InQueueId]);
            context.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                .WithContent(
                    $"{user.Mention} you have been removed from the queue. Please select at least one __region__ and one __league__ using {discordEngine.QueueTracker.Link}.⚠️")
                .AddMention(new UserMention(user.DiscordId))
                .AsEphemeral());
        }
    }
}
