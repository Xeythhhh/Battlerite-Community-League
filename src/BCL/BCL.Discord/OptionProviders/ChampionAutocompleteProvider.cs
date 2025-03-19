using BCL.Domain.Entities.Queue;

using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace BCL.Discord.OptionProviders;

public class ChampionAutocompleteProvider : IAutocompleteProvider
{
#pragma warning disable CS8618
    static IEnumerable<ChampionAutocompleteOption> _champions;
#pragma warning restore CS8618

    public static void Initialize(IEnumerable<Champion> champions) =>
        _champions = champions
            .OrderBy(champion => champion.Role)
            .ThenBy(champion => champion.Class)
            .Select(c => new ChampionAutocompleteOption(c.Name, c.Id));

    public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext context) =>
        Task.FromResult(_champions.Where(champion => string.IsNullOrWhiteSpace(context.OptionValue.ToString())
                                                     || champion.Name.Contains(context.OptionValue.ToString()!, StringComparison.CurrentCultureIgnoreCase))
            .Take(25)
            .Select(champion => new DiscordAutoCompleteChoice(champion.Name, champion.Id)));

    internal class ChampionAutocompleteOption(string name, Ulid id)
    {
        public string NoWhiteSpaceName { get; set; } = name.Replace(" ", "_");
        public string Name { get; set; } = name;
        public string Id { get; set; } = id.ToString();
    }
}
