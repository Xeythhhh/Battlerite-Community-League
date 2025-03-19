using BCL.Domain.Entities.Analytics;

using DSharpPlus.Entities;

namespace BCL.Discord.OptionProviders;
public class SeasonChoiceProvider
{
    // ReSharper disable once StaticMemberInGenericType
#pragma warning disable CS8618
    static IEnumerable<MigrationInfo._Season> _seasons;
#pragma warning restore CS8618

    public static void Initialize(IEnumerable<MigrationInfo._Season> seasons) => _seasons = seasons;

    public Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider() =>
        Task.FromResult(_seasons.Select(season =>
            new DiscordApplicationCommandOptionChoice(
                $"{season.Label}, Started: {season.StartDate:yyyy MM dd}", season.Label)));
}
