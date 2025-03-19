using BCL.Core.Services;
using BCL.Discord.Extensions;

using DSharpPlus.SlashCommands;

// ReSharper disable InconsistentNaming

namespace BCL.Discord.Attributes.Matches;
public class SlashCommand_TeamCaptainAttribute : SlashCheckBaseAttribute
{
    public override Task<bool> ExecuteChecksAsync(InteractionContext context) => TeamCaptainAttribute.ExecuteChecks(context);
}
public class ContextMenu_TeamCaptainAttribute : ContextMenuCheckBaseAttribute
{
    public override Task<bool> ExecuteChecksAsync(ContextMenuContext context) => TeamCaptainAttribute.ExecuteChecks(context);
}

public class TeamCaptainAttribute : BaseAttribute
{
    public static Task<bool> ExecuteChecks(BaseContext context)
    {
        Domain.Entities.Matches.Match? match = MatchService.ActiveMatches.FirstOrDefault(m => m.DiscordUserIds.Contains(context.User.Id));
        bool isCaptain = match?.IsCaptain(context.User.Id, out _) ?? false;
        if (isCaptain) return Task.FromResult(isCaptain);

        string content = $"{context.User.Mention} You need to be a __team captain__ to use {context.Client.MentionCommand(context.CommandName)}!";
        Task.Run(() => Respond(context, content));

        return Task.FromResult(isCaptain);
    }
}
