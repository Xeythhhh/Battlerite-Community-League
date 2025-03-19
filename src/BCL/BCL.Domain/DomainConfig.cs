using Microsoft.Extensions.Configuration;

namespace BCL.Domain;

#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8618
public static class DomainConfig
{
    private static IConfiguration _configuration;

    /// <summary>
    /// Set up Domain configuration
    /// </summary>
    /// <param name="configuration">App config</param>
    public static void Setup(IConfiguration configuration) => _configuration = configuration;

    public static int DefaultRating => _configuration.GetValue<int>("Queue:DefaultRating");
    public static string Season { get; set; } = "Not Initialized";
    public static string ServerAlias => _configuration.GetValue<string>("Queue:Alias");
    public static string ServerName => _configuration.GetValue<string>("Queue:Name");

    public static class Profile
    {
        //Profile
        public static string DefaultColor => _configuration.GetValue<string>("Profile:DefaultColor");
        public static string Version => _configuration.GetValue<string>("Profile:Version");
        public static double DefaultChartAlpha => _configuration.GetValue<double>("Profile:DefaultChartAlpha");
        public static double SessionTimeout => _configuration.GetValue<double>("Profile:SessionTimeout"); //Hours
    }

    public static class Currency
    {
        public static double RegistrationBonus => _configuration.GetValue<double>("Currency:RegistrationBonus");
        public static double Win => _configuration.GetValue<double>("Currency:Win");
        public static double Loss => _configuration.GetValue<double>("Currency:Loss");
        public static double FirstWin => _configuration.GetValue<double>("Currency:FirstWin");
        public static double VipMultiplier => _configuration.GetValue<double>("Currency:VipMultiplier");
        public static double Daily => _configuration.GetValue<double>("Currency:Daily");
        public static string Name => _configuration.GetValue<string>("Currency:Name");
        public static double DailyLimitAmount => _configuration.GetValue<double>("Currency:TransactionLimits:Amount");
        public static double DailyLimitCount => _configuration.GetValue<double>("Currency:TransactionLimits:Count");
        public static string Symbol => _configuration.GetValue<string>("Currency:Symbol");
        public static double DefaultBetAmount => _configuration.GetValue<double>("Currency:DefaultBetAmount");
    }
}
