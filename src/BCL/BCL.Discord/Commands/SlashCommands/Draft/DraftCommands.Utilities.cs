using System.Diagnostics;

using BCL.Common.Extensions;
using BCL.Core.Services;
using BCL.Core.Services.Queue;
using BCL.Discord.Attributes.Matches;
using BCL.Discord.Bot;
using BCL.Discord.Components;
using BCL.Discord.Components.Dashboards;
using BCL.Discord.Components.Draft;
using BCL.Domain.Entities.Matches;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;

#pragma warning disable CS4014

namespace BCL.Discord.Commands.SlashCommands.Draft;
public partial class DraftCommands
{
    [SlashCommandGroup("DraftUtils", "Draft helpers")]
    [SlashModuleLifespan(SlashModuleLifespan.Scoped)]
    public class DraftUtils(
        UserRepository userRepository,
        MatchManager matchManager,
        DiscordEngine discordEngine,
        QueueService queueService) : ApplicationCommandModule
    {
        [SlashCommand_InMatch]
        [SlashCommand("ForceUpdate", "Refresh the Match embed")]
        public async Task ForceUpdate(InteractionContext context)
        {
            await context.DeferAsync();

            DiscordMatch discordMatch = matchManager.GetMatch(context.User) ?? throw new Exception("You are not in a match.");
            switch (discordMatch.Match.GetSide(context.User))
            {
                case Match.Side.Team1:

                    if (context.Channel.Id == discordMatch.Team1.TextChannel.Id) throw new Exception("Use outside of team channel");

                    await discordMatch.Team1.TextChannel.DeleteAsync();
                    discordMatch.Team1.TextChannel = await context.Guild
                        .CreateTextChannelAsync(discordMatch.Team1.Name, context.Guild.GetChannel(DiscordConfig.Channels.QueueCategoryId));

                    await discordMatch.Team1.TextChannel.AddOverwriteAsync(context.Guild.EveryoneRole, deny: Permissions.AccessChannels);

                    foreach (DiscordMember member in discordMatch.Team1.Users
                                 .Select(user => context.Guild.Members[user.DiscordId]))
                    {
                        await discordMatch.Team1.TextChannel.AddOverwriteAsync(member, Permissions.AccessChannels);
                    }

                    foreach (DiscordMember member in discordMatch.Team2.Users
                                 .Select(user => context.Guild.Members[user.DiscordId]))
                    {
                        await discordMatch.Team1.TextChannel.AddOverwriteAsync(member,
                            deny: Permissions.AccessChannels);
                    }

                    discordMatch.Match.Team1ChannelId = discordMatch.Team1.TextChannel.Id;
                    break;

                case Match.Side.Team2:

                    if (context.Channel.Id == discordMatch.Team2.TextChannel.Id) throw new Exception("Use outside of team channel");

                    await discordMatch.Team2.TextChannel.DeleteAsync();
                    discordMatch.Team2.TextChannel = await context.Guild
                        .CreateTextChannelAsync(discordMatch.Team2.Name, context.Guild.GetChannel(DiscordConfig.Channels.QueueCategoryId));

                    await discordMatch.Team1.TextChannel.AddOverwriteAsync(context.Guild.EveryoneRole, deny: Permissions.AccessChannels);

                    foreach (DiscordMember member in discordMatch.Team2.Users
                                 .Select(user => context.Guild.Members[user.DiscordId]))
                    {
                        await discordMatch.Team2.TextChannel.AddOverwriteAsync(member, Permissions.AccessChannels);
                    }

                    foreach (DiscordMember member in discordMatch.Team1.Users
                                 .Select(user => context.Guild.Members[user.DiscordId]))
                    {
                        await discordMatch.Team2.TextChannel.AddOverwriteAsync(member,
                            deny: Permissions.AccessChannels);
                    }

                    discordMatch.Match.Team2ChannelId = discordMatch.Team2.TextChannel.Id;

                    break;

                case Match.Side.None:
                case Match.Side.Both:
                default:
                    throw new UnreachableException();
            }

            await discordMatch.Gate.WaitAsync(5000);
            await discordMatch.Update(false);
            discordMatch.Gate.Release();

            await context.DeleteResponseAsync();
        }

        [SlashCommand_InMatch]
        [SlashCommand_TeamChannel]
        [SlashCommand("ClaimCaptain", "Claim leadership of your team if your captain is taking too long.")]
        public async Task ClaimCaptain(InteractionContext context)
        {
            DiscordMatch discordMatch = matchManager.GetMatch(context.User) ?? throw new UnreachableException();
            if ((DateTime.UtcNow - discordMatch.DraftStartedAt) < TimeSpan.FromMinutes(1))
            {
                await context.CreateResponseAsync($"{context.User.Mention} you can only claim leadership if one minute has elapsed since draft started.");
                return;
            }

            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            ulong captainId = discordMatch.Match.GetSide(context.User) switch
            {
                Match.Side.Team1 => discordMatch.Team1.Captain.DiscordId,
                Match.Side.Team2 => discordMatch.Team2.Captain.DiscordId,
                _ => throw new UnreachableException()
            };

            if (discordMatch.ReadyCheckEntries.Contains(captainId))
            {
                await context.CreateResponseAsync($"{context.User.Mention} you can only claim leadership if your captain has not passed the ReadyCheck.");
                return;
            }

            await discordMatch.Gate.WaitAsync(5000);
            discordMatch.MakeCaptain(context.User);
            discordMatch.Gate.Release();

            await context.CreateResponseAsync($"{context.User.Mention} claimed leadership of this team. :sunglasses:");
        }

        [SlashCommand_InMatch]
        [SlashCommand_TeamChannel]
        [SlashCommand("InviteToDraftChannel", "Invite someone to your draft channel.")]
        public async Task InviteToDraftChannel(InteractionContext context,
            [Option("DiscordUser", "Who would you like to invite to your draft channel?")] DiscordUser discordUser)
        {
            await context.CreateResponseAsync($"Inviting {discordUser.Mention} to your draft...");

            DiscordMember member = discordEngine.Guild.Members[discordUser.Id];
            context.Channel.AddOverwriteAsync(member, Permissions.AccessChannels);
            context.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"{discordUser.Mention} you have been invited to this draft channel by {context.User.Mention}!")
                .AddMention(new UserMention(discordUser.Id)));
        }

        [SlashCommand("Sub", "Sub someone in if anyone is missing.")]
        public async Task Sub(InteractionContext context,
            [Option("AfkUser", "Who is missing?")] DiscordUser afkDiscordUser,
            [Option("DiscordUser", "Who is subbing in?")] DiscordUser subDiscordUser)
        {
            await context.CreateResponseAsync($"Attempting to sub in {subDiscordUser.Mention} for {afkDiscordUser.Mention}...");

            #region Validation

            DiscordMatch? discordMatch = matchManager.GetMatch(afkDiscordUser);
            Domain.Entities.Users.User? sub = userRepository.GetByDiscordId(subDiscordUser.Id);

            string errorMessage = true switch
            {
                true when sub is null => $"{subDiscordUser.Mention} is not registered! {context.User.Mention}",
                true when MatchService.IsInMatch(subDiscordUser.Id) => $"{subDiscordUser.Username} is in an active match! {context.User.Mention}",
                true when discordMatch is null => $"{afkDiscordUser.Username} is not in an active match! {context.User.Mention}",
                true when discordMatch.Match.League is League.Pro => $"Unavailable in **ProLeague** matches! {context.User.Mention}",
                _ => string.Empty
            };

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent(errorMessage)
                    .AddMention(new UserMention(context.User)));
                return;
            }

            Domain.Entities.Users.User afk = userRepository.GetByDiscordId(afkDiscordUser.Id)!;

            #endregion

            #region Confirmation

            DiscordMessageBuilder confirmationPromptMessageBuilder = new DiscordMessageBuilder()
                            .AddEmbed(new DiscordEmbedBuilder()
                                .WithAuthor(sub!.InGameName, null, subDiscordUser.AvatarUrl)
                                .WithDescription($"""
                                      {context.User.Mention} invited you to sub in for {afkDiscordUser.Mention} in {discordMatch!.QueueChannelMessage.JumpLink.DiscordLink("this")} match.
                                      React with :white_check_mark: to accept or :x: to decline
                                      """)
                                .AddField("AFK", $"{afk.Mention}", true)
                                .AddField("Sub", $"{sub.Mention}", true))
                            .WithContent($"{subDiscordUser.Mention}.")
                            .WithAllowedMention(new UserMention(sub.DiscordId));

            DiscordMessage confirmationPrompt = await confirmationPromptMessageBuilder
                .SendAsync(discordEngine.QueueChannel);

            DiscordEmoji confirmEmoji = DiscordEmoji.FromName(context.Client, ":white_check_mark:");
            DiscordEmoji declineEmoji = DiscordEmoji.FromName(context.Client, ":x:");

            confirmationPrompt.CreateReactionAsync(confirmEmoji);
            confirmationPrompt.CreateReactionAsync(declineEmoji);

            QueueTracker.QueueTrackerMessage queueTrackerMessage = new(
                            confirmationPromptMessageBuilder,
                            confirmationPrompt,
                            DateTime.UtcNow.AddMinutes(15));

            discordEngine.QueueTracker.DoNotPurge.Add(queueTrackerMessage);
            try
            {
                await (await discordEngine.Guild.Members[subDiscordUser.Id].CreateDmChannelAsync())
                    .SendMessageAsync($"""
                                       You have been asked by {context.User.Mention} to sub in for {afkDiscordUser.Mention} in match `{discordMatch.Match.Id}`.
                                       Please confirm or decline the request using this {confirmationPrompt.JumpLink.DiscordLink("Confirmation Prompt", "Sub me in!")}.
                                       """); //todo (you can't have markdown links in DMs lol but whatever)
            }
            catch (Exception e)
            {
                discordEngine.Log(e, "`Sending /Sub confirmation prompt in dm.`", false, true);
            }

            InteractivityExtension interactivity = context.Client.GetInteractivity();
            bool validReaction = false;
            while (!validReaction)
            {
                InteractivityResult<DSharpPlus.EventArgs.MessageReactionAddEventArgs> response = await interactivity.WaitForReactionAsync(r =>
                    r.Message.Id == confirmationPrompt.Id
                     && r.User.Id == subDiscordUser.Id, TimeSpan.FromMinutes(2));

                if (response.TimedOut || response.Result.Emoji == declineEmoji)
                {
                    confirmationPrompt.DeleteAsync();
                    context.EditResponseAsync(new DiscordWebhookBuilder().WithContent($":x: Declined{(response.TimedOut ? " (Timed out...)" : "")}"));
                    return;
                }
                if (response.Result.Emoji == confirmEmoji) validReaction = true;
            }

            discordEngine.QueueTracker.DoNotPurge.Remove(queueTrackerMessage);

            if (queueTrackerMessage?.DiscordMessage != null)
                await queueTrackerMessage.DiscordMessage.DeleteAsync();

            context.EditResponseAsync(new DiscordWebhookBuilder().WithContent(":white_check_mark: Accepted"));

            #endregion

            queueService.Leave(sub.DiscordId);
            QueueCommands.ClearPurgeJob(sub.DiscordId);
            discordMatch.Sub(new DiscordMatch.SubPayload(afk, afkDiscordUser), new DiscordMatch.SubPayload(sub, subDiscordUser));
            sub.SubbedIn++;
            await userRepository.SaveChangesAsync();

            discordEngine.Guild.Members[sub.DiscordId].GrantRoleAsync(discordEngine.Guild.Roles[discordMatch.MatchRole.Id]);
            discordEngine.Guild.Members[sub.DiscordId].RevokeRoleAsync(discordEngine.Guild.Roles[DiscordConfig.Roles.InQueueId]);
            discordEngine.Guild.Members[afkDiscordUser.Id].RevokeRoleAsync(discordEngine.Guild.Roles[discordMatch.MatchRole.Id]);

            discordEngine.Log($"Replaced {afkDiscordUser.Mention} with {subDiscordUser.Mention} in match `{discordMatch.Match.Id}` (Triggered by {context.User.Mention}).");
        }

        [SlashCommand_InMatch]
        [SlashCommand_TeamChannel]
        [SlashCommand("RequestVoiceChannel", "Request a team voice channel")]
        public async Task RequestVoiceChannel(InteractionContext context)
        {
            await context.CreateResponseAsync("Requesting a team voice channel...");

            DiscordMatch discordMatch = matchManager.GetMatch(context.User)!;
            Match.Side side = discordMatch.Match.GetSide(context.User);

            DiscordChannel channel;
            switch (side)
            {
                case Match.Side.Team1:

                    if (discordMatch.Team1.VoiceChannel is not null)
                    {
                        await context.EditResponseAsync(new DiscordWebhookBuilder()
                            .WithContent($"You already have a team voice channel {context.User.Mention}! {discordMatch.Team1.VoiceChannel.Mention}"));
                        return;
                    }

                    await discordMatch.Gate.WaitAsync(10000);
                    await discordMatch.Team1.CreateVoiceChannel(context.Guild, discordMatch.Team2, DiscordConfig.Channels.QueueCategoryId);
                    channel = discordMatch.Team1.VoiceChannel!;

                    break;

                case Match.Side.Team2:

                    if (discordMatch.Team2.VoiceChannel is not null)
                    {
                        await context.EditResponseAsync(new DiscordWebhookBuilder()
                            .WithContent($"You already have a team voice channel {context.User.Mention}! {discordMatch.Team2.VoiceChannel.Mention}"));
                        return;
                    }

                    await discordMatch.Gate.WaitAsync(10000);
                    await discordMatch.Team2.CreateVoiceChannel(context.Guild, discordMatch.Team1, DiscordConfig.Channels.QueueCategoryId);
                    channel = discordMatch.Team2.VoiceChannel!;

                    break;

                case Match.Side.None:
                default: throw new UnreachableException();
            }

            discordMatch.Update(false);
            discordMatch.Gate.Release();

            await context.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent($"Enjoy {channel.Mention} :ok_hand:"));
        }
    }

    #region ContextMenu Vote

    [ContextMenu(ApplicationCommandType.MessageContextMenu, "Vote Team 1")]
    public async Task VoteMatchOutcome_T1_Context(ContextMenuContext context)
        => await VoteMatchOutcome(context, MatchOutcome.Team1);
    [ContextMenu(ApplicationCommandType.MessageContextMenu, "Vote Team 2")]
    public async Task VoteMatchOutcome_T2_Context(ContextMenuContext context)
        => await VoteMatchOutcome(context, MatchOutcome.Team2);
    [ContextMenu(ApplicationCommandType.MessageContextMenu, "Vote Drop")]
    public async Task VoteMatchOutcome_Drop_Context(ContextMenuContext context)
        => await VoteMatchOutcome(context, MatchOutcome.Canceled);

    public async Task VoteMatchOutcome(ContextMenuContext context, MatchOutcome outcome)
    {
        await context.CreateResponseAsync($"Handling **Match Context Interaction** for {context.User.Mention}...", true);

        DiscordMatch? discordMatch = matchManager.GetMatchFromMessage(context.TargetMessage);
        discordMatch ??= matchManager.GetMatch(context.User);

        if (discordMatch is null)
        {
            if (context.Channel.IsPrivate)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Nah"));
            }
            else
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                        $"{context.User.Mention} No match found linked to {context.TargetMessage.JumpLink?.DiscordLink("Your Target Message", "The message you right clicked")}!")
                    .AddMention(new UserMention(context.User)));
            }
            return;
        }

        if (outcome != MatchOutcome.Canceled)
        {
            bool draftFinished = discordMatch.Match.Draft.Steps[^1].IsConcluded;
            Match.Side side = discordMatch.Match.GetSide(context.User);
            if (!draftFinished && side is not Match.Side.None)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                        $"{context.User.Mention} You can not vote `{outcome}` for {discordMatch.QueueChannelMessage.JumpLink.DiscordLink("Match", $"Match Message for {discordMatch.Match.Id}")} until the draft is finished!")
                    .AddMention(new UserMention(context.User)));
                return;
            }
        }

        await discordMatch.Vote(context.Interaction, outcome);
    }

    #endregion
}
