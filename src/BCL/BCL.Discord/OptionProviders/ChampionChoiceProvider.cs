using BCL.Domain.Entities.Queue;

using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace BCL.Discord.OptionProviders;
// ReSharper disable once UnusedTypeParameter
public abstract class ChampionChoiceProvider<T> : IChoiceProvider
{
    // ReSharper disable once StaticMemberInGenericType
#pragma warning disable CS8618
    static IEnumerable<Champion> _champions;
#pragma warning restore CS8618

    public static void Initialize(IEnumerable<Champion> champions) => _champions = champions;

    public Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider() =>
        Task.FromResult(_champions
            .Select(champion => new DiscordApplicationCommandOptionChoice(champion.Name, champion.Id.ToString())));
}

public class MeleeChoiceProvider : ChampionChoiceProvider<MeleeChoiceProvider> { }
public class MeleeUnrestrictedChoiceProvider : ChampionChoiceProvider<MeleeUnrestrictedChoiceProvider> { }
public class RangedChoiceProvider : ChampionChoiceProvider<RangedChoiceProvider> { }
public class RangedUnrestrictedChoiceProvider : ChampionChoiceProvider<RangedUnrestrictedChoiceProvider> { }
public class SupportChoiceProvider : ChampionChoiceProvider<SupportChoiceProvider> { }
public class SupportUnrestrictedChoiceProvider : ChampionChoiceProvider<SupportUnrestrictedChoiceProvider> { }
public class DisabledChampionChoiceProvider : ChampionChoiceProvider<DisabledChampionChoiceProvider> { }
