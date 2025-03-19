using System.Diagnostics;

using BCL.Discord.Bot;
using BCL.Discord.Components.Dashboards;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus;
using DSharpPlus.ButtonCommands;
using DSharpPlus.Entities;

#pragma warning disable CS4014
namespace BCL.Discord.Commands.ButtonCommands;

public class AdminButtons(IUserRepository userRepository, DiscordEngine discordEngine) : ButtonCommandModule
{
    public enum RegistrationAction
    {
        Standard,
        Decline
    }

    [ButtonCommand("Register")]
    public async Task Register(ButtonContext context, Ulid userId, RegistrationAction action)
    {
        await context.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .WithContent($"Handling **Registration Interaction** for {context.User.Mention}...")
                .AsEphemeral());

        DiscordMember caller = context.Guild.Members[context.User.Id];
        if (!caller.Roles.Any(r => r.Id == DiscordConfig.Roles.StaffId))
        {
            await context.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"You do not have permission to do that :) {context.User.Mention}")
                .AddMention(new UserMention(context.User)));
            return;
        }

        Domain.Entities.Users.User? user = await userRepository.GetByIdAsync(userId);
        if (user is null)
        {
            await context.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                .WithContent("user was null, @Xeyth"));
            return;
        }
        DiscordMember? member = await context.Guild.GetMemberAsync(user.DiscordId);

        if (member is null)
        {
            await context.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"{user.Mention} is not in the discord server")
                .AddMention(new UserMention(user.DiscordId)));
            return;
        }

        switch (action)
        {
            case RegistrationAction.Standard:
                member.GrantRoleAsync(context.Guild.Roles[DiscordConfig.Roles.MemberId]);
                user.Approved = true;
                user.Pro = false;
                user.ProQueue = false;
                QueueTracker.MembersField.QueueInfos["Standard"].Count++;
                QueueTracker.MembersField.QueueInfos[user.Server.ToString()].Count++;
                break;

            case RegistrationAction.Decline:
                user.Approved = false;
                break;

            default:
                throw new UnreachableException();
        }

        user.RegistrationInfo = $"{action} by {context.User.Id} - {DateTime.UtcNow}";
        user.CurrentSeasonStats.Membership = action switch
        {
            RegistrationAction.Standard => League.Standard,
            RegistrationAction.Decline => League.Custom,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };

        string content = $"Discord: {member.Mention} | InGame:`{user.InGameName}` | StandardMmr:`{user.Rating_Standard}` **{action}** by {caller.Mention}";
        discordEngine.Log(content);
        context.Message.DeleteAsync();
        context.Interaction.DeleteOriginalResponseAsync();
        await userRepository.SaveChangesAsync();
    }
}
