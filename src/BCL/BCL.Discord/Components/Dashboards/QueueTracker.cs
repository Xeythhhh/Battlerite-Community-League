using System.Diagnostics;

using BCL.Common.Extensions;
using BCL.Core.Services.Queue;
using BCL.Discord.Extensions;
using BCL.Discord.Utils;

using DSharpPlus;
using DSharpPlus.ButtonCommands;
using DSharpPlus.Entities;

using Humanizer;

namespace BCL.Discord.Components.Dashboards;

#pragma warning disable CS8618
public partial class QueueTracker
{
    private const int MessageWidth = 48;
    private static string StatusIndicator => $"{ANSIColors.Reset}{GetStatusDecorators()}";

    private static string GetStatusDecorators() => QueueService.Enabled switch
    {
        true when QueueService.TestMode => ANSIColors.Background.Yellow + ANSIColors.Yellow,
        true => ANSIColors.Background.Green + ANSIColors.Green,
        false => ANSIColors.Background.Red + ANSIColors.Red
    };

    private ButtonCommandsExtension _buttonCommandsExtension;
    private DiscordClient _client;
    private string _iconUrl;
    private DateTime _lastPurged;
    private DateTime _lastUpdated;
    private MatchManager _matchManager;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public DiscordChannel QueueChannel { get; set; }
    public QueueTrackerMessage? Description { get; set; }
    public QueueTrackerMessage? Matches { get; set; }
    public QueueTrackerMessage? Users { get; set; }
    public QueueTrackerMessage? Restrictions { get; set; }
    public QueueTrackerMessage? BugRestrictions { get; set; }
    public QueueTrackerMessage? Leagues { get; set; }
    public QueueTrackerMessage? Footer { get; set; }
    public string Link { get; set; } = "__**Queue Tracker**__";
    public List<QueueTrackerMessage> DoNotPurge { get; set; } = [];

    public async Task HangFireRefresh()
    {
        DiscordMessage feedback = await QueueChannel.SendMessageAsync("Hangfire Updating __**Queue Tracker**__(Daily Job)...");
        await Refresh(feedback);
        await feedback.DeleteAsync(10);
    }

    public enum QueueTrackerField
    {
        Description,
        Users,
        Matches,
        Restrictions,
        Leagues,
        Footer,
        All
    }

    private bool _refreshing;

    public async Task Refresh(
        DiscordMessage? feedback = null,
        QueueTrackerField target = QueueTrackerField.All,
        bool skipExpensiveSections = false)
    {
        if (_refreshing) return;
        _refreshing = true;

        try
        {
            if ((DateTime.UtcNow - _lastPurged) > TimeSpan.FromMinutes(15))
                await PurgeQueueChannel();

            Stopwatch stopWatch = Stopwatch.StartNew();
            string info = $"{feedback?.Content}\n__**Tasks**__:";
            TimeSpan duration = TimeSpan.Zero;

            async Task<QueueTrackerMessage> UpdateSection(
                QueueTrackerMessage? message,
                DiscordMessageBuilder builder,
                string sectionName)
            {
                message ??= new QueueTrackerMessage();
                message.Builder = builder;
                await SendOrUpdate(message);
                DoNotPurge.Add(message);
                info += $"\n> - {sectionName} `{stopWatch.Elapsed.Humanize(precision: 3)}`";
                duration += stopWatch.Elapsed;
                stopWatch.Restart();
                return message;
            }

            if (target is QueueTrackerField.Description or QueueTrackerField.All)
                Description = await UpdateSection(Description, DescriptionField.CreateBuilder(), "Description");

            if (target is QueueTrackerField.Matches or QueueTrackerField.All)
            {
                if (!skipExpensiveSections) MatchesField.Refresh();
                Matches = await UpdateSection(Matches, MatchesField.CreateBuilder(), "Updated Match Info");
            }

            if (target is QueueTrackerField.Users or QueueTrackerField.All)
            {
                if (!skipExpensiveSections) MembersField.Refresh();
                Users = await UpdateSection(Users, MembersField.CreateBuilder(), "Updated User Info");
            }

            if (target is QueueTrackerField.Restrictions or QueueTrackerField.All)
            {
                if (!skipExpensiveSections) RestrictionsField.Refresh();

                Restrictions ??= new QueueTrackerMessage();
                BugRestrictions ??= new QueueTrackerMessage();
                List<DiscordMessageBuilder> builders = RestrictionsField.GetBuilders();
                Restrictions.Builder = builders[0];
                await SendOrUpdate(Restrictions);
                DoNotPurge.Add(Restrictions);

                if (builders.Count > 1)
                {
                    BugRestrictions.Builder = builders[1];
                    await SendOrUpdate(BugRestrictions);
                    DoNotPurge.Add(BugRestrictions);
                }

                info += $"\n> - Updated Restrictions `{stopWatch.Elapsed.Humanize(precision: 3)}`";
                duration += stopWatch.Elapsed;
                stopWatch.Restart();
            }

            if (target is QueueTrackerField.Leagues or QueueTrackerField.All)
            {
                if (!skipExpensiveSections) LeagueFields.Refresh();
                Leagues = await UpdateSection(Leagues, LeagueFields.CreateBuilder(), "Updated QueueInfo");
            }

            if (target is QueueTrackerField.Footer or QueueTrackerField.All)
            {
                // TODO ANSIFy
                DiscordMessageBuilder builder = new DiscordMessageBuilder()
                        .WithContent(QueueService.Enabled ? "**Good Luck!**" :
                            $"""
                        ```diff
                        -Queue is currently Disabled ⚠️
                        Reason: {QueueService.DisabledReason}
                        ```
                        """)
                        .AddComponents(QueueButtons)
                        .AddComponents(QueueSettingsButtons);
                if (DiscordConfig.IsTestBot) builder.AddComponents(TestQueueButtons);

                Footer = await UpdateSection(Footer, builder, "Added Footer");
            }

            string content = $"{info}\n\nDone. Duration: `{duration.Humanize(precision: 3)}`";
            if (feedback is not null) await feedback.ModifyAsync(content);

            Link = Description?.DiscordMessage?.JumpLink.DiscordLink("Queue Tracker", "BCL Dashboard")
                ?? throw new Exception("Failed to send Description");
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            _lastUpdated = DateTime.UtcNow;
            _refreshing = false;
        }
    }

    public async Task Setup(
        IServiceProvider services,
        ButtonCommandsExtension btnExt,
        DiscordClient client,
        DiscordChannel queueChannel,
        string iconUrl,
        MatchManager matchManager)
    {
        _buttonCommandsExtension = btnExt;
        _iconUrl = iconUrl;
        _client = client;
        _matchManager = matchManager;
        QueueChannel = queueChannel;

        await PurgeQueueChannel();

        MatchesField.Setup(services);
        MembersField.Setup(services);
        RestrictionsField.Setup(services);
        LeagueFields.Setup(services);

        DiscordMessage feedback = await queueChannel.SendMessageAsync("Setting up __**Queue Tracker**__...");
        await Refresh(feedback);
        QueueService.Queue.CollectionChanged += OnQueueChanged;
        await feedback.DeleteAsync(10);
    }

    private void OnQueueChanged(object? sender, EventArgs eventArgs)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _gate.WaitAsync(5000);
                await Refresh(target: QueueTrackerField.Leagues, skipExpensiveSections: true);
            }
            finally
            {
                _gate.Release();
            }
        });
    }
}
