using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace BCL.Discord.OptionProviders;

public class CommandAutocompleteProvider : IAutocompleteProvider
{
#pragma warning disable CS8618
    private static IEnumerable<DiscordCommand> _commands;
#pragma warning restore CS8618

    public static void Initialize(IReadOnlyList<KeyValuePair<ulong?, IReadOnlyList<DiscordApplicationCommand>>> registeredCommands)
    {
        _commands = registeredCommands.SelectMany(rc => rc.Value)
            .Select(command =>
            {
                List<DiscordCommand> commands = [new DiscordCommand($"/{command.Name}", $"</{command.Name}:{command.Id}>")];

                if (command.Options is not null)
                {
                    commands.AddRange(
                        from subcommand in command.Options
                            .Where(o => o.Type is ApplicationCommandOptionType.SubCommand)
                        where !string.IsNullOrWhiteSpace(subcommand.Name)
                        select new DiscordCommand($"/{command.Name} {subcommand.Name}", $"</{command.Name} {subcommand.Name}:{command.Id}>"));
                }

                return commands;
            })
            .SelectMany(c => c);
    }

    public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext context) =>
        Task.FromResult(_commands.Where(command => string.IsNullOrWhiteSpace(context.OptionValue.ToString())
                                                   || command.DisplayName.Contains(context.OptionValue.ToString()!, StringComparison.CurrentCultureIgnoreCase))
            .Take(25)
            .Select(command => new DiscordAutoCompleteChoice(command.DisplayName, command.Mention))
        );

    internal record DiscordCommand(string DisplayName, string Mention);
}
