using BCL.Common.Extensions;
using BCL.Core.Services;
using BCL.Core.Services.Abstract;
using BCL.Core.Services.Queue;
using BCL.Discord.Attributes.Permissions;
using BCL.Discord.Bot;
using BCL.Discord.Commands.SlashCommands.Users;
using BCL.Discord.Components.Dashboards;
using BCL.Discord.Extensions;
using BCL.Domain;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;

using Hangfire;

#pragma warning disable CS4014

namespace BCL.Discord.Commands.SlashCommands;
[SlashCommandGroup("Queue", "Queue commands")]
[SlashModuleLifespan(SlashModuleLifespan.Scoped)]
public class QueueCommands(IUserRepository userRepository, IQueueService queueService, DiscordEngine discordEngine) : ApplicationCommandModule
{
    [SlashCommand("join", "Join the queue.")]
    public async Task Join(InteractionContext context,
        [Option("role", "Preferred role")] QueueRole role = QueueRole.Fill)
    {
        await context.DeferAsync(true);
        Task.Run(() => _JoinQueue(context.Interaction, context.Client, userRepository, queueService, discordEngine, role));
    }

    [SlashCommand("leave", "Leave the queue.")]
    public async Task Leave(InteractionContext context)
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
            ClearPurgeJob(context.User.Id);
            await context.Member.RevokeRoleAsync(context.Guild.Roles[DiscordConfig.Roles.InQueueId]);
            content = $"{context.User.Mention} __left__ the queue.";
        }

        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .WithContent(content)
                .AddMention(new UserMention(context.User))
                .AsEphemeral());

        await context.DeleteResponseAsync(10);
    }

    [SlashCommand_Staff]
    [SlashCommand("RemoveFromQueue", "Remove a user from the queue.", false)]
    public async Task RemoveFromQueue(InteractionContext context,
        [Option("DiscordUser", "User you want to remove from all queues")] DiscordUser discordUser)
    {
        queueService.Leave(discordUser.Id);
        await context.Guild.Members[discordUser.Id]
            .RevokeRoleAsync(
                context.Guild.Roles[DiscordConfig.Roles.InQueueId]);

        ClearPurgeJob(discordUser.Id);

        string content = $"{context.User.Mention} __removed__ {discordUser.Mention} fom all queues!";
        await context.CreateResponseAsync(content);
        await discordEngine.Log(content);
    }

    [SlashCommand_Staff]
    [SlashCommand("purge", "Purge the queue.", false)]
    public async Task Purge(InteractionContext context,
        [Option("reason", "Reason for purge.")] string? reason = null)
    {
        List<ulong> users = queueService.Purge();
        string content = $"""
                       Queue has been __purged__ by {context.User.Mention} ({users.Count} users)
                       Users: {users.Aggregate("", (current, discordId) => $"{current}<@{discordId}> ")}
                       Reason: {reason ?? "not provided"}
                       """;
        await context.CreateResponseAsync(content);
        await discordEngine.Log(content);

        if (users.Count is not 0)
        {
            foreach (ulong discordId in users)
            {
                await context.Guild.Members[discordId]
                    .RevokeRoleAsync(
                        context.Guild.Roles[DiscordConfig.Roles.InQueueId]);

                ClearPurgeJob(discordId);
            }
        }
    }

    [SlashCommand_Staff]
    [SlashCommand("RefreshQueueChannel", "Purge irrelevant queue channel messages", false)]
    public async Task RefreshQueueChannel(InteractionContext context)
    {
        await context.CreateResponseAsync("Refreshing queue tracker...");
        DiscordMessage feedback = await context.Channel.SendMessageAsync($"Manual **__Queue Tracker__** refresh triggered by {context.User.Mention}");
        discordEngine.QueueTracker.DoNotPurge.Add(new QueueTracker.QueueTrackerMessage(discordMessage: feedback));
        await discordEngine.RefreshQueueTracker(feedback);
        try
        {
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Refreshed {discordEngine.QueueTracker.Link}"));
        }
        catch (NotFoundException) { /* ignored */ } //Probably used the command in #queue channel
        discordEngine.QueueTracker.DoNotPurge.Remove(new QueueTracker.QueueTrackerMessage(discordMessage: feedback));
        await feedback.DeleteAsync(10);
    }

    [SlashCommand_Staff]
    [SlashCommand("Timeout", "Prohibit a user from queueing for a number of days")]
    public async Task Timeout(InteractionContext context,
        [Option("User", "User to time out")] DiscordUser discordUser,
        [Option("Days", "Number of days")] Int64 days,
        [Option("Reason", "Reason for timeout")] string reason)
    {
        User user = userRepository.GetByDiscordId(discordUser.Id) ?? throw new Exception("User is not registered");
        user.Timeout = DateTime.UtcNow.AddDays(days);
        user.TimeoutReason = reason;

        userRepository.SaveChanges();
        string content = $"""
                       {user.Mention} can not queue until {user.Timeout.Value.DiscordTime()}
                       Reason for timeout: '{user.TimeoutReason}'
                       """;
        await context.CreateResponseAsync(content);

        DiscordMessage queueMessage = await discordEngine.QueueChannel.SendMessageAsync(content);
        discordEngine.QueueTracker.DoNotPurge.Add(new QueueTracker.QueueTrackerMessage(discordMessage: queueMessage, timeout: user.Timeout));
        discordEngine.Log(content);
    }

    private static readonly SemaphoreSlim QueueGate = new(6, 6);
    internal static async Task _JoinQueue(
        DiscordInteraction interaction,
        DiscordClient client,
        IUserRepository userRepository,
        IQueueService queueService,
        DiscordEngine discordEngine,
        QueueRole role = QueueRole.Fill,
        bool reply = true)
    {
        await QueueGate.WaitAsync(5000);
        User user = userRepository.GetByDiscordId(interaction.User.Id) ?? throw new Exception("User is not registered");
        if ((user.Timeout ?? DateTime.MinValue) > DateTime.UtcNow) throw new Exception($"Timed out until {user.Timeout}");

        DiscordMember member = interaction.Guild.Members[interaction.User.Id];
        IReadOnlyDictionary<ulong, DiscordRole> guildRoles = interaction.Guild.Roles;

        string errorMessage = true switch
        {
            _ when !user.Approved => $"{user.Mention} Contact __staff__ to be approved!⚠️",
            _ when !QueueService.Enabled => $"""
                                             {user.Mention} the queue is currently __disabled__.⚠️
                                             > Reason: {QueueService.DisabledReason}
                                             """,

            _ when user.ProfileVersion != DomainConfig.Profile.Version ||
                   string.IsNullOrWhiteSpace(user.DefaultMelee) ||
                   string.IsNullOrWhiteSpace(user.DefaultRanged) ||
                   string.IsNullOrWhiteSpace(user.DefaultSupport) ||
                   (user.Approved &&
                    !member.Roles.Contains(guildRoles[DiscordConfig.Roles.MemberId])) => $"{user.Mention} Please update your __profile__ using {client.MentionCommand<UserCommands>(nameof(UserCommands.Settings))}!⚠️",

            _ when user is { Eu: false, Na: false, Sa: false } or { ProQueue: false, StandardQueue: false } => $"{user.Mention} Please select *at least* one __region__ and one __league__ using {discordEngine.QueueTracker.Link}!⚠️",

            _ when queueService.IsUserInQueue(user) => $"{user.Mention} you are already in queue.⚠️",
            _ when MatchService.IsInMatch(user) => $"{user.Mention} you are in a match.⚠️",
            _ => string.Empty
        };

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            if(reply)
            {
                await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                    .WithContent(errorMessage)
                    .AddMention(new UserMention(interaction.User)));
            }
            else
            {
                throw new Exception(errorMessage);
            }
        }

        member.GrantRoleAsync(guildRoles[DiscordConfig.Roles.InQueueId]);

        user.LastQueued = DateTime.UtcNow;
        await userRepository.SaveChangesAsync();

        queueService.Join(user, role);

        try
        {
            if (reply)
            {
                interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"""
                                  {user.Mention} __joined__ the queue as **{role}**.
                                  **Standard**: `{user.StandardQueue}` **Pro**: `{user.ProQueue}`
                                  **EU**: `{user.Eu}` **NA**: `{user.Na}` **SA**: `{user.Sa}`
                                  """)
                    .AddMention(new UserMention(interaction.User)));

                interaction.DeleteOriginalResponseAsync(10);
            }

            EnqueuePurgeJob(user, discordEngine);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        QueueGate.Release();
    }

    internal static async Task _JoinTeamQueue(
        DiscordInteraction interaction,
        DiscordClient client,
        IUserRepository userRepository,
        IQueueService queueService,
        ITeamRepository teamRepository,
        DiscordEngine discordEngine,
        bool reply = true)
    {
        if (reply)
        {
            await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent($"Handling **Queue Interaction** for {interaction.User.Mention}...")
                    .AsEphemeral());
        }

        User? user = userRepository.GetByDiscordId(interaction.User.Id);
        PremadeTeam? team = user is not null && user.TeamId != Ulid.Empty
            ? teamRepository.GetById(user.TeamId)
            : null;
        DiscordMember member = interaction.Guild.Members[interaction.User.Id];
        IReadOnlyDictionary<ulong, DiscordRole> guildRoles = interaction.Guild.Roles;

        #region Guard
        string errorMessage = GuardTeamQueue(interaction, client, queueService, discordEngine, user, team, member, guildRoles);
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                .WithContent(errorMessage)
                .AddMention(new UserMention(interaction.User)));
            return;
        }
        foreach (User teamMember in team!.Members)
            errorMessage = GuardTeamQueue(interaction, client, queueService, discordEngine, teamMember, team, discordEngine.Guild.Members[teamMember.DiscordId], guildRoles);
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                .WithContent(errorMessage)
                .AddMention(new UserMention(interaction.User)));
            return;
        }
        #endregion

        bool notReady = false;
        foreach (User teamMember in team.Members)
        {
            if (queueService.IsUserInQueue(teamMember))
            {
                interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"{teamMember.Mention} is already in queue.")
                    .AddMention(new UserMention(teamMember.DiscordId)));
                notReady = true;
            }

            if (teamMember.TeamId != team.Id)
            {
                interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"{teamMember.Mention} does not have `{team.Name}` as their active team.")
                    .AddMention(new UserMention(teamMember.DiscordId)));
                notReady = true;
            }
        }
        if (notReady) { return; }

        foreach (User teamMember in team.Members)
        {
            interaction.Guild.Members[teamMember.DiscordId]
                .GrantRoleAsync(guildRoles[DiscordConfig.Roles.InQueueId]);
            teamMember.LastQueued = DateTime.UtcNow;
        }
        await userRepository.SaveChangesAsync();

        queueService.Join(user!, QueueRole.Fill, team);

        if (reply)
        {
            interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"""
                              {user!.Mention}'s team __joined__ the queue.
                              **EU**: `{user.Eu}` **NA**: `{user.Na}` **SA**: `{user.Sa}`
                              """)
                .AddMention(new UserMention(interaction.User)));

            interaction.DeleteOriginalResponseAsync(10);
        }
    }

    private static string GuardTeamQueue(DiscordInteraction interaction, DiscordClient client, IQueueService queueService, DiscordEngine discordEngine, User? user, PremadeTeam? team, DiscordMember member, IReadOnlyDictionary<ulong, DiscordRole> guildRoles)
    {
        return true switch
        {
            _ when user is null => $"{interaction.User.Mention} Please __register__ first using {client.MentionCommand<UserCommands>(nameof(UserCommands.Register))}!⚠️",
            _ when !user.Approved => $"{user.Mention} Contact __staff__ to be approved!⚠️",
            _ when !QueueService.Enabled => $"""
                                                     {user.Mention} the queue is currently __disabled__.⚠️
                                                     > Reason: {QueueService.DisabledReason}
                                                     """,
            _ when user.ProfileVersion != DomainConfig.Profile.Version ||
                   string.IsNullOrWhiteSpace(user.DefaultMelee) ||
                   string.IsNullOrWhiteSpace(user.DefaultRanged) ||
                   string.IsNullOrWhiteSpace(user.DefaultSupport) ||
                   (user.Approved &&
                    !member.Roles.Contains(guildRoles[DiscordConfig.Roles.MemberId])) => $"{user.Mention} Please update your __profile__ using {client.MentionCommand<UserCommands>(nameof(UserCommands.Settings))}!⚠️",

            _ when (!user.Eu && user is { Na: false, Sa: false }) ||
                   user is { ProQueue: false, StandardQueue: false } => $"{user.Mention} Please select *at least* one __region__ and one __league__ using {discordEngine.QueueTracker.Link}!⚠️",

            _ when queueService.IsUserInQueue(user) => $"{user.Mention} you are already in queue.⚠️",
            _ when MatchService.IsInMatch(user) => $"{user.Mention} you are in a match.⚠️",
            _ when user.TeamId == Ulid.Empty => $"{user.Mention} you are not in a team.⚠️",
            _ when team is null => $"{user.Mention} you are not in a team.⚠️",
            _ when !team.IsValid => $"{user.Mention} your team is not valid for this queue.⚠️",
            _ => string.Empty
        };
    }

    internal static Dictionary<ulong, string> PurgeJobs { get; set; } = [];
    private static void EnqueuePurgeJob(User user, DiscordEngine discordEngine)
    {
        if (user.PurgeAfter <= 0) return;

        try
        {
            string jobId = BackgroundJob.Schedule(
                () => discordEngine.PurgeUserFromQueue(user.DiscordId),
                DateTime.UtcNow.AddMinutes(user.PurgeAfter));

            PurgeJobs.Add(user.DiscordId, jobId);
        }
        catch (Exception e)
        {
            discordEngine.Log(e, "Trying to register self purge job threw an exception.");
        }
    }
    public static void ClearPurgeJob(ulong discordId)
    {
        if (!PurgeJobs.TryGetValue(discordId, out string? value)) return;

        BackgroundJob.Delete(value);
        PurgeJobs.Remove(discordId);
    }
}
