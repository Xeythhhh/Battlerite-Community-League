using Microsoft.Extensions.Configuration;

namespace BCL.Core;

/// <summary>
/// Core configuration
/// </summary>
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8618
public static class CoreConfig
{
    static IConfiguration _configuration;

    /// <summary>
    /// Set up Core configuration
    /// </summary>
    /// <param name="configuration">App config</param>
    public static void Setup(IConfiguration configuration) => _configuration = configuration;

    public static string Version => _configuration.GetValue<string>("Version");

    /// <summary>
    /// Queue Configuration
    /// </summary>
    public static class Queue
    {
        public static int QueueSize => _configuration.GetValue<int>("Queue:Size");
        public static double RatingShift_Standard => _configuration.GetValue<double>("Queue:RatingShift");
        public static double RatingShift_Pro => _configuration.GetValue<double>("Queue:ProRatingShift");
        public static int PlacementGames => _configuration.GetValue<int>("Queue:PlacementGames");
        public static int ProDropPenalty => _configuration.GetValue<int>("Queue:ProDropPenalty"); //this value has to be divisible by QueueSize
    }

    /// <summary>
    /// Draft Configuration
    /// </summary>
    public static class Draft
    {
        private static string? _proFormatOverride;
        public static string ProFormatRaw
        {
            get => _proFormatOverride ?? _configuration.GetValue<string>("Draft:ProFormat");
            set => _proFormatOverride = value;
        }
        public static List<string> ProFormat => [.. ProFormatRaw.Split("-")];

        private static string? _formatOverride;
        public static string FormatRaw
        {
            get => _formatOverride ?? _configuration.GetValue<string>("Draft:Format");
            set => _formatOverride = value;
        }

        public static List<string> StandardFormat => [.. FormatRaw.Split("-")];

        public static int RequiredReports => _configuration.GetValue<int>("Draft:RequiredReports");

        public static class Sequential
        {
            public static int ActionsPerTurn => _configuration.GetValue<int>("Draft:Sequential:ActionsPerTurn");
            public static int ActionsAtStart => _configuration.GetValue<int>("Draft:Sequential:ActionsAtStart");
            public static string TurnIndicator => _configuration.GetValue<string>("Draft:Sequential:TurnIndicator");
        }
    }
}
