using BCL.Core;
using BCL.Core.Services.Abstract;
using BCL.Discord.Bot;
using BCL.Discord.Commands.SlashCommands;
using BCL.Discord.Commands.SlashCommands.Test;
using BCL.Discord.Extensions;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus;
using DSharpPlus.ButtonCommands;
using DSharpPlus.Entities;

#pragma warning disable CS4014

namespace BCL.Discord.Commands.ButtonCommands;
public class QueueTrackerButtons(
    IQueueService queueService,
    IUserRepository userRepository,
    IChampionRepository championRepository,
    ITeamRepository teamRepository,
    DiscordEngine discordEngine) : ButtonCommandModule
{
    [ButtonCommand("LeaveQueue")]
    public async Task LeaveQueue(ButtonContext context)
    {
        await context.Interaction.DeferAsync(true);
        Task.Run(() => _LeaveQueue(context));
    }

    public async Task _LeaveQueue(ButtonContext context)
    {
        string content = string.Empty;

        if (queueService.IsInPremadeQueue(context.User, out ulong[]? discordIds))
        {
            foreach (ulong discordId in discordIds)
            {
                queueService.Leave(discordId);
                content += $"\n<@{discordId}> __left__ the queue.";
                await context.Guild.Members[discordId]
                    .RevokeRoleAsync(context.Guild.Roles[DiscordConfig.Roles.InQueueId]);
            }
        }
        else
        {
            queueService.Leave(context.User.Id);
            QueueCommands.ClearPurgeJob(context.User.Id);
            await context.Member.RevokeRoleAsync(context.Guild.Roles[DiscordConfig.Roles.InQueueId]);
            content = $"{context.User.Mention} __left__ the queue.";
        }

        await context.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                .WithContent(content));

        await context.Interaction.DeleteOriginalResponseAsync(10);
    }

    [ButtonCommand("JoinQueue")]
    public async Task JoinQueue(ButtonContext context, QueueRole role = QueueRole.Fill)
    {
        await context.Interaction.DeferAsync(true);

        _ = QueueCommands._JoinQueue(
            context.Interaction,
            context.Client,
            userRepository,
            queueService,
            discordEngine,
            role);
    }

    [ButtonCommand("JoinTeamQueue")]
    public async Task JoinTeamQueue(ButtonContext context)
        => await QueueCommands._JoinTeamQueue(
            context.Interaction,
            context.Client,
            userRepository,
            queueService,
            teamRepository,
            discordEngine);

    #region Test Buttons

    [ButtonCommand("TestQueue")]
    public async Task TestQueue(ButtonContext context)
        => await TestCommands._Fill(context.Interaction, CoreConfig.Queue.QueueSize,
            false, true, false, true, true, true, Region.Eu,
            userRepository, championRepository, queueService, context.Client,
            null);

    [ButtonCommand("TestRefresh")]
    public async Task TestRefresh(ButtonContext context)
    {
        if (context.Member.Roles.All(r =>
            r.Id != DiscordConfig.Roles.StaffId &&
            r.Id != DiscordConfig.Roles.SupporterId))
        {
            await discordEngine.QueueChannel.SendMessageAsync(
                new DiscordMessageBuilder()
                    .WithContent($"{context.User.Mention} you do not have permission to use this."));
            return;
        }

        await context.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .WithContent($"Refreshing {discordEngine.QueueTracker.Link}"));

        DiscordMessage feedback = await context.Channel.SendMessageAsync("Refreshing __**QueueTracker**__ (Test Button)...");
        await discordEngine.RefreshQueueTracker(feedback);
        await feedback.DeleteAsync(10);
        await context.Interaction.DeleteOriginalResponseAsync();
    }

    #endregion
}
