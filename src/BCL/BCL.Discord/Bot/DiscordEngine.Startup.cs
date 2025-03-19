using BCL.Core.Services;
using BCL.Core.Services.Abstract;
using BCL.Discord.Commands.ButtonCommands;
using BCL.Discord.Commands.ModalCommands;
using BCL.Discord.Commands.SlashCommands;
using BCL.Discord.Commands.SlashCommands.Admin;
using BCL.Discord.Commands.SlashCommands.Draft;
using BCL.Discord.Commands.SlashCommands.Test;
using BCL.Discord.Commands.SlashCommands.Users;
using BCL.Discord.Components;
using BCL.Discord.Components.Dashboards;
using BCL.Discord.Converters;
using BCL.Discord.OptionProviders;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus;
using DSharpPlus.ButtonCommands;
using DSharpPlus.ButtonCommands.EventArgs;
using DSharpPlus.ButtonCommands.Extensions;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.ModalCommands;
using DSharpPlus.ModalCommands.EventArgs;
using DSharpPlus.ModalCommands.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;

using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS4014

namespace BCL.Discord.Bot;
public partial class DiscordEngine
{
    private readonly IServiceProvider _services;
    private readonly MatchManager _matchManager;
    private readonly HttpClient _httpClient;
    private bool _ready;
    private static bool _running; //this is for testing right now

    public DiscordClient Client { get; set; }
    public QueueTracker QueueTracker { get; set; }
    public ProLeagueManager ProLeagueManager { get; set; }

    #region Modules

    public InteractivityExtension Interactivity { get; set; }
    public SlashCommandsExtension SlashCommands { get; set; }
    public ModalCommandsExtension ModalCommands { get; set; }
    public ButtonCommandsExtension ButtonCommands { get; set; }

    #endregion

    #region Snowflake objects

    public DiscordGuild Guild { get; set; }
    public DiscordChannel QueueChannel { get; set; }
    public DiscordChannel AdminChannel { get; set; }
    public DiscordChannel ExceptionChannel { get; set; }
    public DiscordChannel DroppedMatchesChannel { get; set; }
    public DiscordChannel MatchHistoryChannel { get; set; }
    public DiscordChannel LogChannel { get; set; }
    public DiscordChannel DebugChannel { get; set; }
    public DiscordChannel AttachmentsChannel { get; set; }
    public DiscordChannel TransactionLog { get; set; }

    #endregion

#pragma warning disable CS8618
    public DiscordEngine(
        IServiceProvider services,
        MatchManager matchManager,
        QueueTracker queueTracker,
        HttpClient httpClient,
        ProLeagueManager proLeagueManager)
#pragma warning restore CS8618
    {
        _services = services;
        _matchManager = matchManager;
        _httpClient = httpClient;
        QueueTracker = queueTracker;
        MatchService.MatchStarted += OnMatchStarted;
        //BankService.GuildTransaction += OnGuildTransaction;
        ProLeagueManager = proLeagueManager;
    }

    public async Task Start()
    {
        // TODO investigate
        if (_running) return; _running = true; // test for a weird multi instance bug

        _services.CreateScope().ServiceProvider.GetRequiredService<IStatsService>().StartupRoutine();

        await Log("Discord Starting...", true);
        SetupClient();
        RegisterSeasons();
        SetupOptionProviders();

        await Log("Registering commands...", true);
        RegisterSlashCommands();
        RegisterModalCommands();
        RegisterButtonCommands();
        RegisterEvents();

        Client.ClientErrored += Client_ClientErrored;
        Client.SocketErrored += Client_SocketErrored;

        await Client.ConnectAsync();
    }

    private Task Client_SocketErrored(DiscordClient sender, DSharpPlus.EventArgs.SocketErrorEventArgs e)
    {
        Console.WriteLine(e.Exception);
        return Task.CompletedTask;
    }

    private Task Client_ClientErrored(DiscordClient sender, DSharpPlus.EventArgs.ClientErrorEventArgs e)
    {
        Console.WriteLine(e.Exception);
        return Task.CompletedTask;
    }

    private void RegisterSeasons()
    {
        using IServiceScope scope = _services.CreateScope();
        IAnalyticsRepository analyticsRepository = scope.ServiceProvider.GetRequiredService<IAnalyticsRepository>();

        Domain.Entities.Analytics.MigrationInfo migrationInfo = analyticsRepository.GetMigrationInfo()!;
        string output = string.Empty;
        AdminCommands.Dev.RegisterSeasons(scope.ServiceProvider.GetRequiredService<IMatchRepository>().GetAllCompleted(),
            migrationInfo, ref output);
        analyticsRepository.SaveChanges();

        Log(output.Trim(), true, true);
    }

    #region Setup

    public void SetupClient()
    {
        Console.WriteLine("asdjghsajd");
        Client = new DiscordClient(DiscordConfig.ClientConfiguration);
        Interactivity = Client.UseInteractivity(DiscordConfig.InteractivityConfiguration);
        SlashCommands = Client.UseSlashCommands(DiscordConfig.GetSlashCommandsConfiguration(_services));
        ModalCommands = Client.UseModalCommands(DiscordConfig.GetModalCommandsConfiguration(_services));
        ButtonCommands = Client.UseButtonCommands(DiscordConfig.GetButtonCommandsConfiguration(_services));
    }

    private void RegisterEvents()
    {
        Log("Registering events...", true);
        Client.Ready += OnClientReady;
        Client.ComponentInteractionCreated += OnComponentInteractionCreated;
        Client.ClientErrored += OnClientErrored;
        Client.GuildDownloadCompleted += OnGuildDownloadCompleted;
        Client.Zombied += OnClientZombied;
    }
    private void RegisterButtonCommands()
    {
        ButtonCommands.RegisterButtons<AdminButtons>();
        ButtonCommands.RegisterButtons<QueueTrackerButtons>();
        ButtonCommands.RegisterButtons<UserSettingsButtons>();
        ButtonCommands.RegisterButtons<MatchButtons>();
        ButtonCommands.RegisterButtons<ProLeagueButtons>();
        ButtonCommands.RegisterConverter(new UlidConverter());
        ButtonCommands.RegisterConverter(new EnumConverter<AdminButtons.RegistrationAction>());
        ButtonCommands.RegisterConverter(new EnumConverter<UserSettingsButtons.QueueSetting>());
        ButtonCommands.RegisterConverter(new EnumConverter<QueueRole>());
        ButtonCommands.RegisterConverter(new EnumConverter<MatchOutcome>());
        ButtonCommands.RegisterConverter(new EnumConverter<Region>());
        ButtonCommands.ButtonCommandErrored += OnButtonCommandErrored;
        ButtonCommands.ButtonCommandExecuted += OnButtonCommandExecuted;
    }

    private async Task OnButtonCommandExecuted(ButtonCommandsExtension buttonCommands, ButtonCommandExecutionEventArgs args)
        => await Log($"{args.Context.User.Id} | {args.Context.User.Username} executed {args.ButtonId}", true);

    private void RegisterModalCommands()
    {
        ModalCommands.RegisterModals<UserModals>();
        ModalCommands.RegisterModals<AdminModals>();
        ModalCommands.RegisterConverter(new UlidConverter());
        ModalCommands.ModalCommandErrored += OnModalCommandErrored;
        ModalCommands.ModalCommandExecuted += OnModalCommandExecuted;
    }

    private async Task OnModalCommandExecuted(ModalCommandsExtension sender, ModalCommandExecutionEventArgs args)
        => await Log($"{args.Context.User.Id} | {args.Context.User.Username} executed {args.ModalId}", true);

    private void RegisterSlashCommands()
    {
        SlashCommands.RegisterCommands<QueueCommands>();
        SlashCommands.RegisterCommands<DraftCommands>();
        SlashCommands.RegisterCommands<AdminCommands>();
        SlashCommands.RegisterCommands<UserCommands>();
        if (DiscordConfig.IsTestBot) SlashCommands.RegisterCommands<TestCommands>();

        SlashCommands.SlashCommandErrored += OnSlashCommandErrored;
        SlashCommands.SlashCommandExecuted += OnSlashCommandExecuted;
        SlashCommands.SlashCommandInvoked += OnSlashCommandInvoked;
        CommandAutocompleteProvider.Initialize(SlashCommands.RegisteredCommands);
    }

    private async Task OnSlashCommandInvoked(SlashCommandsExtension sender, SlashCommandInvokedEventArgs args)
        => await Log($"{args.Context.User.Id} | {args.Context.User.Username} invoked {args.Context.CommandName}", true);

    private async Task OnSlashCommandExecuted(SlashCommandsExtension sender, SlashCommandExecutedEventArgs args)
        => await Log($"{args.Context.User.Id} | {args.Context.User.Username} executed {args.Context.CommandName}", true);

    public void SetupOptionProviders()
    {
        using IServiceScope scope = _services.CreateScope();
        IChampionRepository championRepository = scope.ServiceProvider.GetRequiredService<IChampionRepository>();
        List<Domain.Entities.Queue.Champion> champions = championRepository.GetAllEnabled().ToList();
        ChampionAutocompleteProvider.Initialize(champions);

        IEnumerable<Domain.Entities.Queue.Champion> melee = champions.Where(c => c is { Class: ChampionClass.Melee, Role: ChampionRole.Dps });
        IEnumerable<Domain.Entities.Queue.Champion> ranged = champions.Where(c => c is { Class: ChampionClass.Ranged, Role: ChampionRole.Dps });
        IEnumerable<Domain.Entities.Queue.Champion> support = champions.Where(c => c.Role == ChampionRole.Healer);
        MeleeChoiceProvider.Initialize(melee);
        RangedChoiceProvider.Initialize(ranged);
        SupportChoiceProvider.Initialize(support);

        IEnumerable<Domain.Entities.Queue.Champion> disabled = championRepository.GetAllDisabled();
        DisabledChampionChoiceProvider.Initialize(disabled);

        List<Domain.Entities.Queue.Champion> unrestrictedChampions = championRepository.GetAll().ToList();
        IEnumerable<Domain.Entities.Queue.Champion> unrestrictedMelee = unrestrictedChampions.Where(c => c is { Class: ChampionClass.Melee, Role: ChampionRole.Dps });
        IEnumerable<Domain.Entities.Queue.Champion> unrestrictedRanged = unrestrictedChampions.Where(c => c is { Class: ChampionClass.Ranged, Role: ChampionRole.Dps });
        IEnumerable<Domain.Entities.Queue.Champion> unrestrictedSupport = unrestrictedChampions.Where(c => c.Role == ChampionRole.Healer);
        MeleeUnrestrictedChoiceProvider.Initialize(unrestrictedMelee);
        RangedUnrestrictedChoiceProvider.Initialize(unrestrictedRanged);
        SupportUnrestrictedChoiceProvider.Initialize(unrestrictedSupport);

        IMapRepository mapRepository = scope.ServiceProvider.GetRequiredService<IMapRepository>();
        MapChoiceProvider.Initialize(mapRepository.GetAll().ToList());

        SeasonChoiceProvider.Initialize(scope.ServiceProvider.GetRequiredService<IAnalyticsRepository>()
            .GetMigrationInfo()!.Seasons);

        Log("DiscordClient finished registering option providers.", true);
    }

    #endregion
}
