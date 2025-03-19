using BCL.Domain.Entities.Queue;

using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace BCL.Discord.OptionProviders;

public class MapChoiceProvider : IChoiceProvider
{
    static IEnumerable<Map> _maps = new List<Map>();

    public static void Initialize(IEnumerable<Map> maps) => _maps = maps;

    public Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider()
    {
        return Task.FromResult(_maps
            .Select(m => new DiscordApplicationCommandOptionChoice(m.Name, m.Id.ToString()))
            .AsEnumerable());
    }
}
