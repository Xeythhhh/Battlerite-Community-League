using DSharpPlus;
using DSharpPlus.ButtonCommands;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.ModalCommands;
using DSharpPlus.SlashCommands;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BCL.Discord;

/// <summary>
/// Discord Engine configuration class
/// </summary>
public static class DiscordConfig
{
#pragma warning disable CS8618
    static IConfiguration _configuration;
#pragma warning restore CS8618

    /// <summary>
    /// Set up Discord Configuration
    /// </summary>
    /// <param name="configuration">App config</param>
    public static void Setup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Returns the Discord Client Configuration
    /// </summary>
    public static DiscordConfiguration ClientConfiguration =>
        new()
        {
            MinimumLogLevel = LogLevel.Error,
            Token = _configuration.GetConnectionString("DiscordToken"),
            TokenType = TokenType.Bot,
            AutoReconnect = true,
            Intents = DiscordIntents.All
        };

    /// <summary>
    /// Returns the configuration object for Interactivity Module
    /// </summary>
    public static InteractivityConfiguration InteractivityConfiguration =>
        new()
        {
            Timeout = TimeSpan.FromMinutes(10),
            ButtonBehavior = ButtonPaginationBehavior.DeleteMessage,
            PaginationBehaviour = PaginationBehaviour.Ignore,
        };

    /// <summary>
    /// Returns the configuration object for SlashCommands Module
    /// </summary>
    public static SlashCommandsConfiguration GetSlashCommandsConfiguration(IServiceProvider services)
        => new() { Services = services };

    public static ModalCommandsConfiguration GetModalCommandsConfiguration(IServiceProvider services)
        => new() { Services = services };

    public static ButtonCommandsConfiguration GetButtonCommandsConfiguration(IServiceProvider services)
        => new()
        {
            ArgumentSeparator = "|",
            Services = services
        };

    public static ulong ServerId => _configuration.GetValue<ulong>("Discord:ServerId");
    public static string StartupStatus => _configuration.GetValue<string>("Discord:StartupStatus")!;

    public static class Channels
    {
        public static ulong QueueId => _configuration.GetValue<ulong>("Discord:Channels:Queue");
        public static ulong AdminDashboardId => _configuration.GetValue<ulong>("Discord:Channels:AdminDashboard");
        public static ulong ExceptionId => _configuration.GetValue<ulong>("Discord:Channels:Exception");
        public static ulong DebugId => _configuration.GetValue<ulong>("Discord:Channels:Debug");
        public static ulong MatchHistoryId => _configuration.GetValue<ulong>("Discord:Channels:MatchHistory");
        public static ulong DroppedMatchesId => _configuration.GetValue<ulong>("Discord:Channels:DroppedMatches");
        public static ulong LogId => _configuration.GetValue<ulong>("Discord:Channels:Log");
        public static ulong AttachmentsId => _configuration.GetValue<ulong>("Discord:Channels:Attachments");
        public static ulong GeneralVoiceId => _configuration.GetValue<ulong>("Discord:Channels:GeneralVoice");
        public static ulong QueueCategoryId => _configuration.GetValue<ulong>("Discord:Channels:QueueCategory");
        public static ulong SupporterCategoryId => _configuration.GetValue<ulong>("Discord:Channels:SupporterChannels");
        public static ulong TransactionLog => _configuration.GetValue<ulong>("Discord:Channels:TransactionLog");

        public static class Pro
        {
            public static ulong GeneralId => _configuration.GetValue<ulong>("Discord:Channels:Pro:General");
            public static ulong EuId => _configuration.GetValue<ulong>("Discord:Channels:Pro:EU");
            public static ulong NaId => _configuration.GetValue<ulong>("Discord:Channels:Pro:NA");
            public static ulong SaId => _configuration.GetValue<ulong>("Discord:Channels:Pro:SA");

            public static class Admin
            {
                // ReSharper disable MemberHidesStaticFromOuterClass
                public static ulong EuId => _configuration.GetValue<ulong>("Discord:Channels:Pro:Admin:EU");
                public static ulong NaId => _configuration.GetValue<ulong>("Discord:Channels:Pro:Admin:NA");
                public static ulong SaId => _configuration.GetValue<ulong>("Discord:Channels:Pro:Admin:SA");
                // ReSharper restore MemberHidesStaticFromOuterClass
            }
        }
    }

    public static class Roles
    {
        public static ulong MemberId => _configuration.GetValue<ulong>("Discord:Roles:Member");
        public static ulong ProId => _configuration.GetValue<ulong>("Discord:Roles:Pro");
        public static ulong SupporterId => _configuration.GetValue<ulong>("Discord:Roles:Supporter");
        public static ulong StaffId => _configuration.GetValue<ulong>("Discord:Roles:Staff");
        public static ulong InQueueId => _configuration.GetValue<ulong>("Discord:Roles:InQueue");
        public static ulong QueuesEuId => _configuration.GetValue<ulong>("Discord:Roles:EU");
        public static ulong QueuesNaId => _configuration.GetValue<ulong>("Discord:Roles:NA");
        public static ulong QueuesSaId => _configuration.GetValue<ulong>("Discord:Roles:SA");
        public static ulong QueuesProId => _configuration.GetValue<ulong>("Discord:Roles:ProQueue");
        public static ulong QueuesStandardId => _configuration.GetValue<ulong>("Discord:Roles:StandardQueue");
        public static ulong SupportersAboveId => _configuration.GetValue<ulong>("Discord:Roles:SupporterRolesAbove");

        public static class Region
        {
            public static ulong EuId => _configuration.GetValue<ulong>("Discord:Roles:Region:EU");
            public static ulong NaId => _configuration.GetValue<ulong>("Discord:Roles:Region:NA");
            public static ulong SaId => _configuration.GetValue<ulong>("Discord:Roles:Region:SA");
        }
    }

    //Misc
    public static ulong DevId => _configuration.GetValue<ulong>("Discord:DevId");
    public static ulong[] AdminIds => _configuration.GetSection("Discord:AdminIds").Get<ulong[]>()!;
    public static bool IsTestBot => _configuration.GetValue<bool>("Discord:IsTestBot");
    public static int QueueTrackerMinUpdateRate => _configuration.GetValue<int>("Discord:QueueTrackerMinUpdateRate");

    public static class HangfireDatabase
    {
        public static string ConnectionString => _configuration.GetConnectionString("HangfireConnectionString")!;
    }
}
