using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

using BCL.Discord.Extensions;

using DSharpPlus;
using DSharpPlus.ButtonCommands;
using DSharpPlus.ButtonCommands.EventArgs;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.ModalCommands;
using DSharpPlus.ModalCommands.EventArgs;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;

namespace BCL.Discord.Bot;
[SuppressMessage("Roslynator", "RCS1163:Unused parameter", Justification = "<Pending>")]
[SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
public partial class DiscordEngine
{
    #region Events

    private async Task OnSlashCommandErrored(SlashCommandsExtension sender, SlashCommandErrorEventArgs e)
    {
        if (e.Exception is NotFoundException) return;

        (string response, bool sendToDebug, bool followUp) = await GetLoggingSettings(e);
        string additionalInfo = $"**SlashCommandErrored** | {sender.Client.MentionCommand(e.Context.CommandName)} | by{e.Context.User.Mention} | InteractionId:`{e.Context.InteractionId}`";

        //TODO better options after researching command group structures 
        List<DiscordInteractionDataOption>? options = e.Context.Interaction.Data.Options?.ToList();
        if (options != null)
        {
            string optionsData = "";
            foreach (DiscordInteractionDataOption? option in options)
            {
                string subOptionsData = string.Empty;
                if (option.Type is ApplicationCommandOptionType.SubCommand or ApplicationCommandOptionType.SubCommandGroup)
                {
                    List<DiscordInteractionDataOption>? subOptions = option.Options?.ToList();
                    if (subOptions != null)
                    {
                        subOptionsData = "\n";
                        foreach (DiscordInteractionDataOption? subOption in subOptions)
                        {
                            string subSubOptionsData = string.Empty;
                            if (subOption.Type is ApplicationCommandOptionType.SubCommand)
                            {
                                List<DiscordInteractionDataOption>? subSubOptions = subOption.Options?.ToList();
                                if (subSubOptions != null)
                                {
                                    subSubOptionsData = subSubOptions.Aggregate("\n", (current, subSubOption) => $"{current}        ({subSubOption.Type}){subSubOption.Name}: {subSubOption.Value}\n");
                                }
                            }
                            subOptionsData = $"{subOptionsData}    ({subOption.Type}){subOption.Name}: {subOption.Value}{subSubOptionsData}\n";
                        }
                    }
                }
                optionsData = $"{optionsData}({option.Type}){option.Name}: {option.Value}{subOptionsData}\n";
            }

            string optionsInfo = $"""
                                | Options:
                               ```ml
                               {optionsData}

                               ```
                               """;
            additionalInfo += optionsInfo;
        }

#pragma warning disable CS4014
        Log(e.Exception, additionalInfo, false, sendToDebug);
#pragma warning restore CS4014

        try
        {
            if (followUp)
            {
                await e.Context.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                                .WithContent(response));
            }
            else
            {
                await e.Context.Interaction.Channel.SendMessageAsync(new DiscordMessageBuilder()
                                .WithContent(response));
            }
        }
        catch { /* ignored */ }
    }

    private async Task OnModalCommandErrored(ModalCommandsExtension sender, ModalCommandErrorEventArgs e)
    {
        if (e.Exception is NotFoundException) return;

        (string response, bool sendToDebug, bool followUp) = await GetLoggingSettings(e);

        string additionalInfo = $"""
                              **ModalCommandErrored** | CommandName:`{e.CommandName}` | ModalId:`{e.ModalId}` | by{e.Context.User.Mention} | Values:
                              ```
                              {e.Context.Values.Aggregate("", (current, val) => current + $"{val}\n")}
                              ```
                              """;

#pragma warning disable CS4014
        Log(e.Exception, additionalInfo, false, sendToDebug);
#pragma warning restore CS4014
        try
        {
            if (followUp)
            {
                await e.Context.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                                .WithContent(response));
            }
            else
            {
                await e.Context.Interaction.Channel.SendMessageAsync(new DiscordMessageBuilder()
                                .WithContent(response));
            }
        }
        catch { /* ignored */ }
    }
    private async Task OnButtonCommandErrored(ButtonCommandsExtension sender, ButtonCommandErrorEventArgs e)
    {
        if (e.Exception is NotFoundException) return;

        (string response, bool sendToDebug, bool followUp) = await GetLoggingSettings(e);

        string additionalInfo = $"""
                              **ButtonCommandErrored** | CommandName:`{e.CommandName}` | ButtonId:`{e.ButtonId}` | by{e.Context.User.Mention} | Values:
                              ```
                              {e.Context.Values.Aggregate("", (current, val) => current + $"{val}\n")}
                              ```
                              """;

#pragma warning disable CS4014
        Log(e.Exception, additionalInfo, false, sendToDebug);
#pragma warning restore CS4014
        try
        {
            if (followUp)
            {
                await e.Context.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                                .WithContent(response));
            }
            else
            {
                await e.Context.Interaction.Channel.SendMessageAsync(new DiscordMessageBuilder()
                                    .WithContent(response));
            }
        }
        catch { /* ignored */ }
    }

    #endregion

    /// <summary>
    /// Logs to the console and LogChannel
    /// </summary>
    /// <param name="message">Message to log</param>
    /// <param name="noChannelMessage">`True` if you don't want a channel message, this is useful before channels get initialized</param>
    /// <param name="sendToDebugChannel">`True` if you want to log this to DebugChannel instead of LogChannel</param>
    /// <returns></returns>
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable IDE0060 // Remove unused parameter
    public async Task Log(string message, bool noChannelMessage = false, bool sendToDebugChannel = false)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning restore IDE0079 // Remove unnecessary suppression
    {
        DateTime timeStamp = DateTime.UtcNow;
        Console.WriteLine($"[{timeStamp}] {message}");
        return;

        //if (noChannelMessage) return;

        //var channel = sendToDebugChannel ? DebugChannel : LogChannel;
        //try
        //{
        //    await channel.SendMessageAsync(new DiscordMessageBuilder()
        //        .WithContent($"{timeStamp.DiscordTime(DiscordTimeFlag.R)} {message}")
        //        .WithAllowedMention(new UserMention(Client.CurrentUser)));
        //}
        //catch (Exception e)
        //{
        //    if (e is NullReferenceException) return;
        //    Console.WriteLine(e);
        //}
    }

    /// <summary>
    /// Logs to the console and ExceptionChannel todo eventually to a real logger
    /// </summary>
    /// <param name="exception">Exception to log</param>
    /// <param name="additionalInfo">Information about the exception context</param>
    /// <param name="noChannelMessage">`True` if you don't want a channel message, this is useful before channels get initialized</param>
    /// <param name="sendToDebugChannel">`True` if you want to log this to DebugChannel instead of ExceptionChannel</param>;
    /// <returns></returns>
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable IDE0060 // Remove unused parameter
    public async Task Log(Exception exception, string? additionalInfo = null, bool noChannelMessage = false, bool sendToDebugChannel = false)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning restore IDE0079 // Remove unnecessary suppression
    {
        DateTime timeStamp = DateTime.UtcNow;
        Console.WriteLine($"[{timeStamp}] {additionalInfo}\n{exception}");

        return;
        //if (noChannelMessage) return;

        //var channel = sendToDebugChannel ? DebugChannel : ExceptionChannel;

        //DiscordException? discordException = null;
        //switch (exception)
        //{
        //    case NotFoundException notFoundException:
        //        discordException = notFoundException;
        //        additionalInfo += " | Errors:`Interaction Not Found`";
        //        break;

        //    case BadRequestException badRequestException:
        //        discordException = badRequestException;
        //        if (!string.IsNullOrWhiteSpace(badRequestException.Errors))
        //            additionalInfo += $" | Errors:`{badRequestException.Errors}`";
        //        break;

        //    case DiscordException ex: discordException = ex; break;
        //}

        //if (discordException is not null)
        //{
        //    if (!string.IsNullOrWhiteSpace(discordException.Message))
        //        additionalInfo += $" | Message:`{discordException.Message}`";
        //    if (!string.IsNullOrWhiteSpace(discordException.JsonMessage))
        //        additionalInfo += $" | JsonMessage:`{discordException.JsonMessage}`";
        //}

        //try
        //{
        //    var exStackTrace = GetFromException(exception, ex => GetStackTraceInfo(ex), "", "\n\n");
        //    exStackTrace = exStackTrace.Length > 1500
        //        ? exStackTrace[..1500]
        //        : exStackTrace; //discord limits message length to 2000 

        //    var sources = $"Source: {GetFromException(exception, ex => ex.Source, "'", " | ")}";
        //    var messages = GetFromException(exception, ex => ex.Message);
        //    var types = $"Type: {GetFromException(exception, ex => ex.GetType().ToString(), "'", " | ")}";

        //    var content = $"""
        //                   {timeStamp.DiscordTime(DiscordTimeFlag.R)}
        //                   {additionalInfo}
        //                   ```csharp
        //                   {sources}
        //                   {types}

        //                   {messages}
        //                   ```
        //                   ```csharp
        //                   {exStackTrace}
        //                   ```
        //                   """;
        //    await channel.SendMessageAsync(content);
        //}
        //catch (Exception e)
        //{
        //    Console.WriteLine(e);
        //}
    }

    private static async Task<(string, bool, bool)> GetLoggingSettings(ButtonCommandErrorEventArgs buttonCommandErrorEventArgs) => await GetLoggingSettings(buttonCommandErrorEventArgs.Context.Interaction, buttonCommandErrorEventArgs.Exception);
    private static async Task<(string, bool, bool)> GetLoggingSettings(ModalCommandErrorEventArgs modalCommandErrorEventArgs) => await GetLoggingSettings(modalCommandErrorEventArgs.Context.Interaction, modalCommandErrorEventArgs.Exception);
    private static async Task<(string, bool, bool)> GetLoggingSettings(SlashCommandErrorEventArgs slashCommandErrorEventArgs) => await GetLoggingSettings(slashCommandErrorEventArgs.Context.Interaction, slashCommandErrorEventArgs.Exception);
    private static async Task<(string, bool, bool)> GetLoggingSettings(DiscordInteraction interaction, Exception exception)
    {
        (string response, bool sendToDebug) = exception switch
        {
            NotFoundException => ($"Interaction `{interaction.Id}` not found.", true),
            SlashExecutionChecksFailedException => ("You do not have permissions to use this command", true),
            ContextMenuExecutionChecksFailedException => ("You do not have permissions to use this command", true),
            _ => ($"""
                   Command Errored. Exception Details:
                   {exception.Source}
                   ```diff
                   -{exception.Message}{(exception.InnerException is not null ? $"\n-{exception.InnerException?.Message}" : string.Empty)}
                   ```
                   """, false)
        };

        bool followUp;
        try { followUp = await interaction.GetOriginalResponseAsync() is not null; } //code is shit and if you remove this it will break
        catch { followUp = false; }

        return (response, sendToDebug, followUp);
    }
    private static string GetFromException(Exception exception, Expression<Func<Exception, string?>> expr, string decorator = "", string separator = "\n")
    {
        string fromException = GetFromException(false, exception, expr, decorator, separator);
        return fromException[(fromException.IndexOf(separator, StringComparison.Ordinal) + separator.Length)..].Trim();
    }
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedParameter.Local
    private static string GetFromException(bool doNotUseThisMethod_useOverload, Exception exception, Expression<Func<Exception, string?>> expr, string decorator = "", string separator = "\n")
        => $"{(exception.InnerException is not null ? GetFromException(doNotUseThisMethod_useOverload, exception.InnerException, expr, decorator, separator) : string.Empty)}{separator}{decorator}{expr.Compile()(exception)}{decorator}";
    private static string GetStackTraceInfo(Exception? exception) =>
        exception is null
            ? string.Empty
            : HonestlyIHaveNoIdeaWhatThisIs()
                .Matches(exception.StackTrace ?? string.Empty)
                .Aggregate("", (current, match) =>
                    $"{current}{(match.Value.Contains(".cs:line") ? $"\n    in {match.Value}" : $"\n{match.Value.Replace(".", " ")})")}");

    [GeneratedRegex(@"(([^.]*(\.)){0,1}[^.]*\({1})|[^\\]*(\.cs[^\n]*)")]
    private static partial Regex HonestlyIHaveNoIdeaWhatThisIs();

    //public async Task OnGuildTransaction(object sender, GuildTransactionEventArgs args) =>
    //    await LogTransaction(args.DiscordId, args.Snapshot);

    //public async Task LogTransaction(User user, ulong? recipientDiscordId = null) =>
    //    await LogTransaction(user.DiscordId, user.LatestBalanceSnapshot, recipientDiscordId);

    //public async Task LogTransaction(ulong discordId, User.BalanceSnapshot snapshot, ulong? recipientDiscordId = null)
    //{
    //    var value = Math.Abs(snapshot.Amount);
    //    var (sign, indicator) = snapshot.Amount switch
    //    {
    //        > 0 => ("+ ", "<----"),
    //        < 0 => ("- ", "---->"),
    //        _ => (string.Empty, string.Empty)
    //    };

    //    var emoji = snapshot.Amount switch
    //    {
    //        < 0 => ":scream_cat:",
    //        > 0 and < 10 => ":coin:",
    //        >= 10 and < 50 => ":money_with_wings:",
    //        >= 50 and < 100 => ":money_mouth:",
    //        >= 100 and < 500 => ":moneybag:",
    //        >= 500 and < 1000 => ":credit_card:",
    //        >= 1000 => ":chart:",
    //        _ => string.Empty
    //    };

    //    var recipientInfo = recipientDiscordId is null ? string.Empty : $" {indicator} <@{recipientDiscordId}>";
    //    var content = $"""
    //                   {DateTime.UtcNow.DiscordTime(DiscordTimeFlag.R)} {emoji} <@{discordId}>{recipientInfo}
    //                   ```diff
    //                   {sign}{value.ToGuildCurrencyString()} | {snapshot.Info}```
    //                   """;

    //    //Temporary workaround for removing the mentions while I look into how to do it more cleanly
    //    var msg = await TransactionLog.SendMessageAsync("...");
    //    await msg.ModifyAsync(content);
    //}
}
