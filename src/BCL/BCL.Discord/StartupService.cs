using BCL.Discord.Bot;
using BCL.Discord.Components;
using BCL.Discord.Components.Dashboards;
using BCL.Discord.Components.Draft;

using Hangfire;
using Hangfire.LiteDB;
using Hangfire.Storage;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BCL.Discord;

public static class StartupService
{
    public static void AddDiscordEngine(this IServiceCollection services)
    {
        services.AddSingleton<HttpClient>();
        services.AddSingleton<MatchManager>();
        services.AddSingleton<QueueTracker>();
        services.AddSingleton<ProLeagueManager>();
        services.AddScoped<DraftEngine>();
        services.AddSingleton<DiscordEngine>();
    }

    public static void AddHangfire(this IServiceCollection services)
    {
        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseLiteDbStorage($"Filename={AppDomain.CurrentDomain.BaseDirectory}\\{DiscordConfig.HangfireDatabase.ConnectionString}"));

        services.AddHangfireServer();
    }

    public static async Task UseDiscord(this IApplicationBuilder app)
    {
        foreach (RecurringJobDto? recurringJob in JobStorage.Current.GetConnection().GetRecurringJobs()
                     .Where(recurringJob => recurringJob.Id is "DeleteTimedOutSnowFlakeObjects" or "RefreshQueueChannel"))
        {
            RecurringJob.RemoveIfExists(recurringJob.Id);
        }

        using IServiceScope scope = app.ApplicationServices.CreateScope();

        await scope.ServiceProvider.GetRequiredService<DiscordEngine>().Start();
    }
}
