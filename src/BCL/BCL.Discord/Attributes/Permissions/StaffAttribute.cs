using BCL.Discord.Extensions;

using DSharpPlus.SlashCommands;

// ReSharper disable InconsistentNaming

namespace BCL.Discord.Attributes.Permissions;
public class SlashCommand_StaffAttribute : SlashCheckBaseAttribute
{
    public override async Task<bool> ExecuteChecksAsync(InteractionContext context) => await StaffAttribute.ExecuteChecks(context);
}

public class ContextMenu_StaffAttribute : ContextMenuCheckBaseAttribute
{
    public override async Task<bool> ExecuteChecksAsync(ContextMenuContext context) => await StaffAttribute.ExecuteChecks(context);
}

public class StaffAttribute : BaseAttribute
{
    public static Task<bool> ExecuteChecks(BaseContext context)
    {
        bool isStaff = !context.Channel.IsPrivate && context.Member.Roles.Any(r => r.Id == DiscordConfig.Roles.StaffId);
        if (isStaff) return Task.FromResult(isStaff);

        string content = context.Channel.IsPrivate
            ? "Unavailable in dms."
            : $"{context.User.Mention} You need to be a __Staff__ member to use {context.Client.MentionCommand(context.CommandName)}!";
        Task.Run(() => Respond(context, content));

        return Task.FromResult(isStaff);
    }
}
