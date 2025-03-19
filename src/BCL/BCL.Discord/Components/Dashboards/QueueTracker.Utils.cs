
using System.Reflection;
using System.Text.RegularExpressions;

using BCL.Discord.Utils;

namespace BCL.Discord.Components.Dashboards;
public partial class QueueTracker
{
    private static string Wrap(string input) => $"""
        ```ANSI
        {input.Split("\n").Aggregate("", (current, next) => $"{current}\n{StatusIndicator}█{next}")}
        ```
        """;

    private static int GetWidth(string input) => ANSIEscapeRegex().Replace(input, "").Length;

    private async Task SendOrUpdate(QueueTrackerMessage message) =>
        message.DiscordMessage = message.DiscordMessage == null
            ? await (message.Builder?.SendAsync(QueueChannel)
                ?? throw new InvalidOperationException("No Builder found for message"))
            : await message.DiscordMessage.ModifyAsync(message.Builder);

    private async Task PurgeQueueChannel()
    {
        _lastPurged = DateTime.UtcNow;

        List<DSharpPlus.Entities.DiscordMessage> messagesToDelete = (await QueueChannel.GetMessagesAsync())
            .Where(msgToDelete => DoNotPurge
                .Where(dnp => dnp.Timeout is null || dnp.Timeout < DateTime.UtcNow)
                .Select(dnp => dnp.DiscordMessage)
                .Concat(_matchManager.Messages)
                .All(doNotDelete => doNotDelete?.Id != msgToDelete.Id))
            .ToList();

        if (messagesToDelete.Count != 0) await QueueChannel.DeleteMessagesAsync(messagesToDelete);
    }

    public static string GetAnsiColor(string propertyName)
    {
        Type type = typeof(ANSIColors);
        FieldInfo? fieldInfo = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        if (fieldInfo != null && fieldInfo.FieldType == typeof(string))
        {
            return (string?)fieldInfo.GetValue(null) ?? ANSIColors.Red;
        }
        else
        {
            throw new ArgumentException($"Property {propertyName} not found or is not of type string. ");
        }
    }

    [GeneratedRegex(@"\u001b\[\d{1,2}(;\d{1,2})?m")]
    internal static partial Regex ANSIEscapeRegex();
}
