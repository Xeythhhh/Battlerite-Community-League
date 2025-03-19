using BCL.Discord.Extensions;

using DSharpPlus.SlashCommands;

// ReSharper disable InconsistentNaming

namespace BCL.Discord.Attributes.Permissions;
public class SlashCommand_SupporterAttribute : SlashCheckBaseAttribute
{
    public override async Task<bool> ExecuteChecksAsync(InteractionContext context)
        => await SupporterAttribute.ExecuteChecks(context);
}
public class ContextMenu_SupporterAttribute : ContextMenuCheckBaseAttribute
{
    public override async Task<bool> ExecuteChecksAsync(ContextMenuContext context)
        => await SupporterAttribute.ExecuteChecks(context);
}

public class SupporterAttribute : BaseAttribute
{
    public static Task<bool> ExecuteChecks(BaseContext context)
    {
        bool isSupporter = !context.Channel.IsPrivate && context.Member.Roles.Any(r => r.Id == DiscordConfig.Roles.SupporterId);
        if (isSupporter) return Task.FromResult(isSupporter);

        string content = context.Channel.IsPrivate
            ? "Unavailable in dms."
            : $"{context.User.Mention} You need to be a __Supporter__ to use {context.Client.MentionCommand(context.CommandName)}!";
        Task.Run(() => Respond(context, content));

        return Task.FromResult(isSupporter);
    }
}
