using BCL.Discord.Extensions;

using DSharpPlus.SlashCommands;

// ReSharper disable InconsistentNaming

namespace BCL.Discord.Attributes.Permissions;
public class SlashCommand_DevAttribute : SlashCheckBaseAttribute
{
    public override Task<bool> ExecuteChecksAsync(InteractionContext context) => DevAttribute.ExecuteChecks(context);
}
public class ContextMenu_DevAttribute : ContextMenuCheckBaseAttribute
{
    public override Task<bool> ExecuteChecksAsync(ContextMenuContext context) => DevAttribute.ExecuteChecks(context);
}

public class DevAttribute : BaseAttribute
{
    public static Task<bool> ExecuteChecks(BaseContext context)
    {
        bool isDev = context.User.Id == DiscordConfig.DevId;
        if (isDev) return Task.FromResult(isDev);

        string content = $"{context.User.Mention} You need to be <@{DiscordConfig.DevId}> to use {context.Client.MentionCommand(context.CommandName)}!";
        Task.Run(() => Respond(context, content));

        return Task.FromResult(isDev);
    }
}
