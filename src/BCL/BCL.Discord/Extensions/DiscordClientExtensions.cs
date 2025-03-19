using System.Reflection;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace BCL.Discord.Extensions;

public static class DiscordClientExtensions
{
    public static string MentionCommand<T>(this DiscordClient client, string suggestedCommand)
    {
        MethodInfo? methodInfo = typeof(T).GetMethod(suggestedCommand);
        SlashCommandAttribute? attribute = methodInfo?.GetCustomAttribute<SlashCommandAttribute>(true);
        return client.MentionCommand(
            attribute?.Name ??
            suggestedCommand);
    }

    public static string MentionCommand(this DiscordClient client, string keyword, bool exactMatch = true)
    {
        List<_Command> commands = client.GetSlashCommands().RegisteredCommands
            .SelectMany(rc => rc.Value)
            .Select(c => new _Command(c))
            .ToList();

        return commands.FirstOrDefault(c => c.Match(keyword, exactMatch))?._Mention
               ?? throw new Exception("Invalid Command");
    }

    // ReSharper disable once InconsistentNaming
    internal class _Command(DiscordApplicationCommand appCommand)
    {
        public DiscordApplicationCommand AppCommand { get; set; } = appCommand;
        public List<DiscordApplicationCommandOption> SubCommands { get; set; } = appCommand.Options?.Where(o =>
                o.Type is ApplicationCommandOptionType.SubCommand).ToList() ?? [];

        // ReSharper disable once InconsistentNaming
        public string? _Mention;
        public bool Match(string keyword, bool exactMatch)
        {
            bool isAppCommand = exactMatch
                ? AppCommand.Name.Equals(keyword, StringComparison.CurrentCultureIgnoreCase)
                : AppCommand.Name.Contains(keyword, StringComparison.CurrentCultureIgnoreCase);
            if (isAppCommand) _Mention = $"</{AppCommand.Name}:{AppCommand.Id}>";

            DiscordApplicationCommandOption? subCommand = SubCommands.FirstOrDefault(s => exactMatch
                ? s.Name.Equals(keyword, StringComparison.CurrentCultureIgnoreCase)
                : s.Name.Contains(keyword, StringComparison.CurrentCultureIgnoreCase));
            if (subCommand is not null) _Mention = $"</{AppCommand.Name} {subCommand.Name}:{AppCommand.Id}>";

            return isAppCommand || (subCommand is not null);
        }
    }
}
