using BCL.Core.Services;
using BCL.Discord.Extensions;

using DSharpPlus.SlashCommands;

// ReSharper disable InconsistentNaming

namespace BCL.Discord.Attributes.Matches;
public class SlashCommand_TeamChannelAttribute : SlashCheckBaseAttribute
{
    public override Task<bool> ExecuteChecksAsync(InteractionContext context) => TeamChannelAttribute.ExecuteChecks(context);
}
public class ContextMenu_TeamChannelAttribute : ContextMenuCheckBaseAttribute
{
    public override Task<bool> ExecuteChecksAsync(ContextMenuContext context) => TeamChannelAttribute.ExecuteChecks(context);
}

public class TeamChannelAttribute : BaseAttribute
{
    public static Task<bool> ExecuteChecks(BaseContext context)
    {
        ulong teamChannelId = MatchService.GetTeamChannelId(context.User.Id);
        bool isInTeamChannel = teamChannelId == context.Channel.Id;
        if (isInTeamChannel) return Task.FromResult(isInTeamChannel);

        string content = $"{context.User.Mention} You need to be in <#{teamChannelId}> to use {context.Client.MentionCommand(context.CommandName)}!";
        Task.Run(() => Respond(context, content));

        return Task.FromResult(isInTeamChannel);
    }
}
