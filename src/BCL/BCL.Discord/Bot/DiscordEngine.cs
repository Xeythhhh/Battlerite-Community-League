using System.Diagnostics;

using BCL.Common.Extensions;
using BCL.Core.Services.Queue;
using BCL.Discord.Commands.SlashCommands;
using BCL.Discord.Commands.SlashCommands.Users;
using BCL.Discord.Components.Dashboards;
using BCL.Domain.Entities.Queue;
using BCL.Domain.Entities.Users;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;

using Hangfire;

using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS4014

namespace BCL.Discord.Bot;
public partial class DiscordEngine
{
    #region Discord Client Events

    private static bool _notifyZombied = true;
    private async Task OnClientZombied(DiscordClient sender, ZombiedEventArgs e)
    {
        Log("**ClientZombied** I'm a zombie.....", _notifyZombied);
        _notifyZombied = false;

        await Client.ReconnectAsync(true);
    }
    private Task OnClientErrored(DiscordClient sender, ClientErrorEventArgs e)
    {
        if (e.Exception is NotFoundException) return Task.CompletedTask;

        string content = $"**ClientErrored** Event: `{e.EventName}`";
        if (e.Exception is DiscordException discordError)
            content += $" | JsonMessage: `{discordError.JsonMessage}`";

        Log(e.Exception, content);
        return Task.CompletedTask;
    }
    private Task OnClientReady(DiscordClient sender, ReadyEventArgs e)
    {
        QueueTracker.Status = DiscordConfig.StartupStatus;
        Client.UpdateStatusAsync(new DiscordActivity(DiscordConfig.StartupStatus), null, DateTimeOffset.UtcNow);
        Log("DiscordClient ready.", true);
        return Task.CompletedTask;
    }
    private async Task OnGuildDownloadCompleted(DiscordClient sender, GuildDownloadCompletedEventArgs e)
    {
        Log("DiscordClient finished downloading Guild data.", true);

        Guild = await Client.GetGuildAsync(DiscordConfig.ServerId);
        Log("Getting LogChannel", true);
        LogChannel = await Client.GetChannelAsync(DiscordConfig.Channels.LogId);
        Log("Getting ExceptionChannel", true);
        ExceptionChannel = await Client.GetChannelAsync(DiscordConfig.Channels.ExceptionId);
        Log("Getting DebugChannel", true);
        DebugChannel = await Client.GetChannelAsync(DiscordConfig.Channels.DebugId);

        Log("Getting QueueChannel", true, true);
        QueueChannel = await Client.GetChannelAsync(DiscordConfig.Channels.QueueId);
        Log("Getting AdminChannel", true, true);
        AdminChannel = await Client.GetChannelAsync(DiscordConfig.Channels.AdminDashboardId);
        Log("Getting MatchHistoryChannel", true, true);
        MatchHistoryChannel = await Client.GetChannelAsync(DiscordConfig.Channels.MatchHistoryId);
        Log("Getting DroppedMatchesChannel", true, true);
        DroppedMatchesChannel = await Client.GetChannelAsync(DiscordConfig.Channels.DroppedMatchesId);
        Log("Getting AttachmentsChannel", true, true);
        AttachmentsChannel = await Client.GetChannelAsync(DiscordConfig.Channels.AttachmentsId);
        Log("Getting TransactionLogChannel", true, true);
        TransactionLog = await Client.GetChannelAsync(DiscordConfig.Channels.TransactionLog);

        Log("Setting up Queue Tracker", false, true);
        await QueueTracker.Setup(_services,
            ButtonCommands,
            Client,
            QueueChannel,
            //DebugChannel,
            //LogChannel,
            Guild.IconUrl,
            _matchManager);
        Log("Finished setting up Queue Tracker", false, true);

        Log("Setting up Pro League Manager", false, true);
        ProLeagueManager.Setup(
            _services,
            await Client.GetChannelAsync(DiscordConfig.Channels.Pro.GeneralId),
            await Client.GetChannelAsync(DiscordConfig.Channels.Pro.EuId),
            await Client.GetChannelAsync(DiscordConfig.Channels.Pro.NaId),
            await Client.GetChannelAsync(DiscordConfig.Channels.Pro.SaId),
            await Client.GetChannelAsync(DiscordConfig.Channels.Pro.Admin.EuId),
            await Client.GetChannelAsync(DiscordConfig.Channels.Pro.Admin.NaId),
            await Client.GetChannelAsync(DiscordConfig.Channels.Pro.Admin.SaId));
        Log("Finished setting up Pro League Manager", false, true);

        Log("Removing InQueue role", false, true);
        foreach (DiscordMember? discordMember in (await Guild.GetAllMembersAsync()).Where(m => m.Roles.Any(r => r.Id == DiscordConfig.Roles.InQueueId)))
            await discordMember.RevokeRoleAsync(Guild.Roles[DiscordConfig.Roles.InQueueId]);

        RecurringJob.AddOrUpdate(
            "DeleteTimedOutSnowFlakeObjects",
            () => DeleteTimedOutSnowFlakeObjects(),
            Cron.Daily);

        RecurringJob.AddOrUpdate(
            "RefreshQt",
            () => QueueTracker.HangFireRefresh(),
            Cron.Daily);

        _ready = true;

        Log("DiscordClient finished registering recurring jobs.", false, true);
        IEnumerable<DiscordApplicationCommand> commands = SlashCommands.RegisteredCommands.SelectMany(rc => rc.Value);
        Log($"Registered __{commands.Count()}__ **SlashCommands**.", false, true);
        Log($"Registered __{ModalCommands.RegisteredCommands.Count}__ **ModalCommands**.", false, true);
    }
    private async Task OnComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        try
        {
            switch (e.Id)
            {
                case { } s when s.StartsWith("SeasonSelect"):
                    await HandleSeasonSelectInteraction(e);
                    return;

                    //default: Log($"Unknown Component interaction: {e.Id} by {e.User.Username}");
                    //    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    //        new DiscordInteractionResponseBuilder()
                    //            .WithContent($"Unknown Interaction `{e.Id}`")
                    //            .AsEphemeral());

                    //return;
            }
        }
        catch (Exception exception)
        {
            Log(exception);
            await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"""
                 Interaction `{e.Id}` failed. ```diff
                 {exception.Message}```
                 """));
        }
    }

    private async Task HandleSeasonSelectInteraction(ComponentInteractionCreateEventArgs e)
    {
        await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .WithContent("Updating embed...")
                .AsEphemeral());

        using IServiceScope scope = _services.CreateScope();

        string[] args = e.Id.Split('|');
        UserCommands.SelectFor componentType = Enum.Parse<UserCommands.SelectFor>(args[1]);
        Ulid entityId = Ulid.Parse(args[2]);
        ulong componentOwner = ulong.Parse(args[3]);
        string[] selectedOptions = e.Values;

        User? user = null;
        Champion? champion = null;
        DiscordUser? discordUser = null;
        IEnumerable<QueueTracker.MatchesField.Filter>? queueInfo = null;

        switch (componentType)
        {
            case UserCommands.SelectFor.Profile /*or UserCommands.SelectFor.Wallet*/:
                user = await scope.ServiceProvider.GetRequiredService<IUserRepository>().GetByIdAsync(entityId);
                discordUser = await Client.GetUserAsync(user!.DiscordId);
                break;

            case UserCommands.SelectFor.ChampionProfile:
                champion = await scope.ServiceProvider.GetRequiredService<IChampionRepository>().GetByIdAsync(entityId);
                break;

            case UserCommands.SelectFor.Leaderboard:
                queueInfo = QueueTracker.MatchesField.QueueInfos.Where(q =>
                    q.FilteredBy is QueueTracker.MatchesField.Filter.FilterType.League
                                 or QueueTracker.MatchesField.Filter.FilterType.Region);
                break;
        }

        IEnumerable<UserCommands.SelectOption> options = componentType switch
        {
            UserCommands.SelectFor.Profile/* or UserCommands.SelectFor.Wallet*/ =>
                UserCommands.SelectOption.GetOptions(user!),

            UserCommands.SelectFor.ChampionProfile =>
                UserCommands.SelectOption.GetOptions(champion!),

            UserCommands.SelectFor.ChampionStats =>
                UserCommands.SelectOption.GetOptions(
                    scope.ServiceProvider.GetRequiredService<IAnalyticsRepository>().GetMigrationInfo()!, true),

            UserCommands.SelectFor.Leaderboard =>
                UserCommands.SelectOption.GetOptions(
                    scope.ServiceProvider.GetRequiredService<IAnalyticsRepository>().GetMigrationInfo()!, true, queueInfo),

            _ => UserCommands.SelectOption.GetOptions(
                scope.ServiceProvider.GetRequiredService<IAnalyticsRepository>().GetMigrationInfo()!)
        };

        DiscordEmbed embed = componentType switch
        {
            UserCommands.SelectFor.Profile => await UserCommands
                .GetProfileEmbedAsync(discordUser!, user!, selectedOptions, this),

            //UserCommands.SelectFor.Wallet => await UserCommands
            //    .GetWalletEmbedAsync(discordUser!, user!, selectedOptions, this),

            UserCommands.SelectFor.RegionStats => UserCommands.Stats
                .GetRegionStatsEmbed(UserCommands.Stats.GetGenericEmbed(Guild.IconUrl), selectedOptions, _services),

            UserCommands.SelectFor.ChampionStats => UserCommands.Stats
                .GetChampionStatsEmbed(UserCommands.Stats.GetGenericEmbed(Guild.IconUrl), selectedOptions, _services),

            UserCommands.SelectFor.ChampionProfile => await UserCommands.Stats
                .GetChampionProfileEmbedAsync(champion!, UserCommands.Stats.GetGenericEmbed(Guild.IconUrl), selectedOptions, this),

            UserCommands.SelectFor.Leaderboard => UserCommands.UserUtils
                .GetLeaderboard(UserCommands.Stats.GetGenericEmbed(Guild.IconUrl), user, selectedOptions, _services),

            _ => throw new UnreachableException()
        };

        DiscordSelectComponent components = componentType switch
        {
            _ => UserCommands.BuildFilterSelectComponent(entityId, e.User.Id, Guild.Emojis, selectedOptions, options, componentType),
        };

        if (e.User.Id != componentOwner)
        {
            e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                .WithContent(
                    $"""
                     You are not the original author of __{e.Message.JumpLink.DiscordLink("this embed")}__, here's another one!
                     If you want an editable one instead of a copy, request the embed yourself!
                     ***Ephemeral** (only visible to you) embeds will always return a copy.* {e.User.Mention}
                     """)
                .AddEmbed(embed)
                .AddComponents(components));
            return;
        }

        if (e.Message.Flags is MessageFlags.Ephemeral)
        {
            e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                .WithContent(
                    $"**Ephemeral** (only visible to you) embeds can not be edited, here's an updated new one! {e.User.Mention}")
                .AddEmbed(embed)
                .AddComponents(UserCommands.BuildFilterSelectComponent(entityId, componentOwner, Guild.Emojis, selectedOptions, options, componentType)));
            return;
        }

        await e.Message.ModifyAsync(m =>
        {
            m.Embed = embed;
            m.AddComponents(components);
        });

        await e.Interaction.DeleteOriginalResponseAsync();
    }

    #endregion

    #region Cleanup

    public void DeleteTimedOutSnowFlakeObjects()
    {
        if (!_ready) return;
        DeleteTimedOutMatchRoles();
        DeleteTimedOutMatchChannels();
    }
    private async Task DeleteTimedOutMatchRoles()
    {
        Log("Deleting timed out match roles started...", true);
        List<DiscordRole> roles = Guild.Roles.Where(role =>
                Ulid.TryParse(role.Value.Name.Split("_").Last(), out _)
                && _matchManager.Roles.All(r => r.Id != role.Value.Id))
            .Select(r => r.Value)
            .ToList();

        if (roles.Count is 0) { Log("No roles deleted.", true); return; }

        List<Task> tasks = roles.ConvertAll(role => role.DeleteAsync("Timed out"));
        await Task.WhenAll(tasks);
        Log($"{roles.Count} timed out match roles deleted.");
    }
    private async Task DeleteTimedOutMatchChannels()
    {
        Log("Deleting timed out match channels started...", true);
        List<DiscordChannel> channels = Guild.Channels.Where(channel =>
                //this is shit but it gets the job done for now, don't @me
                //channels without a parent should not be deleted hence the fallback value
                //only care about channels in Queue Category
                (channel.Value.ParentId ?? DiscordConfig.Channels.QueueCategoryId) == DiscordConfig.Channels.QueueCategoryId
                //don't delete threads of #queue
                && (channel.Value.ParentId ?? 123) != DiscordConfig.Channels.QueueId
                //don't delete #queue
                && channel.Value.Id != DiscordConfig.Channels.QueueId
                && !channel.Value.IsCategory
                //don't delete active match channels
                && _matchManager.Channels.All(c => c.Id != channel.Value.Id))
            .Select(c => c.Value)
            .ToList();

        if (channels.Count is 0) { Log("No channels deleted.", true); return; }

        List<Task> tasks = channels.ConvertAll(channel => channel.DeleteAsync("Timed out"));
        await Task.WhenAll(tasks);
        Log($"{channels.Count} timed out match channels deleted.");
    }

    public async Task RefreshQueueTracker(DiscordMessage feedback, bool skipExpensiveSections = false)
    {
        if (!_ready) return;
        DeleteTimedOutSnowFlakeObjects();
        await QueueTracker.Refresh(feedback, QueueTracker.QueueTrackerField.All, skipExpensiveSections);
    }

    public void PurgeUserFromQueue(ulong userId)
    {
        if (!QueueCommands.PurgeJobs.ContainsKey(userId)) return;

        QueueCommands.PurgeJobs.Remove(userId);
        DiscordMember member = Guild.Members[userId];

        QueueService._Leave(userId);
        member.RevokeRoleAsync(Guild.Roles[DiscordConfig.Roles.InQueueId]);

        try
        {
            DiscordDmChannel dmChannel = member.CreateDmChannelAsync().GetAwaiter().GetResult();
            dmChannel.SendMessageAsync("Your inactivity timer expired, you have been removed from the queue. Feel free to re-queue!");
        }
        catch
        {
            QueueChannel.SendMessageAsync($"{member.Mention} Your inactivity timer expired, you have been removed from the queue. Feel free to re-queue!");
        }
    }

    #endregion
}
