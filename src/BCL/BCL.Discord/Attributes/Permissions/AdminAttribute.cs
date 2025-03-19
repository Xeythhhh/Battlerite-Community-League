using BCL.Discord.Extensions;

using DSharpPlus.SlashCommands;

// ReSharper disable InconsistentNaming

namespace BCL.Discord.Attributes.Permissions;
public class SlashCommand_AdminAttribute : SlashCheckBaseAttribute
{
    public override Task<bool> ExecuteChecksAsync(InteractionContext context) => AdminAttribute.ExecuteChecks(context);
}
public class ContextMenu_AdminAttribute : ContextMenuCheckBaseAttribute
{
    public override Task<bool> ExecuteChecksAsync(ContextMenuContext context) => AdminAttribute.ExecuteChecks(context);
}

public class AdminAttribute : BaseAttribute
{
    public static Task<bool> ExecuteChecks(BaseContext context)
    {
        bool isAdmin = DiscordConfig.AdminIds.Any(i => i == context.User.Id) && !context.Channel.IsPrivate;
        if (isAdmin) return Task.FromResult(isAdmin);

        string content = $"{context.User.Mention} You need to be an __administrator__ to use {context.Client.MentionCommand(context.CommandName)}!";
        Task.Run(() => Respond(context, content));

        return Task.FromResult(isAdmin);
    }
}
