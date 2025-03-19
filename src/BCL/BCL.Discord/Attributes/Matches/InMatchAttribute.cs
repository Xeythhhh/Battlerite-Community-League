using BCL.Core.Services;
using BCL.Discord.Extensions;

using DSharpPlus.SlashCommands;

// ReSharper disable InconsistentNaming

namespace BCL.Discord.Attributes.Matches;
public class SlashCommand_InMatchAttribute : SlashCheckBaseAttribute
{
    public override Task<bool> ExecuteChecksAsync(InteractionContext context) => InMatchAttribute.ExecuteChecks(context);
}
public class ContextMenu_InMatchAttribute : ContextMenuCheckBaseAttribute
{
    public override Task<bool> ExecuteChecksAsync(ContextMenuContext context) => InMatchAttribute.ExecuteChecks(context);
}

public class InMatchAttribute : BaseAttribute
{
    public static Task<bool> ExecuteChecks(BaseContext context)
    {
        bool inMatch = MatchService.IsInMatch(context.User.Id);
        if (inMatch) return Task.FromResult(inMatch);

        string content = $"{context.User.Mention} You need to be in an __active match__ to use {context.Client.MentionCommand(context.CommandName)}!";
        Task.Run(() => Respond(context, content));

        return Task.FromResult(inMatch);
    }
}
