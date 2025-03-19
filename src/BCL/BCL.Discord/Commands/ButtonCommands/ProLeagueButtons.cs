using System.Diagnostics;
using BCL.Discord.Bot;
using BCL.Discord.Components;
using DSharpPlus;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;
using DSharpPlus.ButtonCommands;
using DSharpPlus.Entities;

namespace BCL.Discord.Commands.ButtonCommands;
internal class ProLeagueButtons(ProLeagueManager proLeagueManager, IUserRepository userRepository) : ButtonCommandModule
{
    [ButtonCommand("ProApplicationVote")]
    public async Task ProApplicationVote(ButtonContext context, ulong applicantId, bool approve)
    {
        await context.Interaction.DeferAsync(true);

        Region region;
        ulong regionRoleId;
        if (context.Channel.Id == DiscordConfig.Channels.Pro.EuId)
        {
            region = Region.Eu;
            regionRoleId = DiscordConfig.Roles.Region.EuId;
        }
        else if (context.Channel.Id == DiscordConfig.Channels.Pro.NaId)
        {
            region = Region.Na;
            regionRoleId = DiscordConfig.Roles.Region.NaId;
        }
        else if (context.Channel.Id == DiscordConfig.Channels.Pro.SaId)
        {
            region = Region.Sa;
            regionRoleId = DiscordConfig.Roles.Region.SaId;
        }
        else
        {
            throw new UnreachableException("Button should not exist outside of Pro Channels/Threads");
        }

        if (context.Member.Roles.All(r => r.Id != regionRoleId))
            throw new InvalidOperationException("User is not a member of this region");

        if (context.Member.Roles.All(r => r.Id != DiscordConfig.Roles.ProId))
            throw new InvalidOperationException("User is not a member of this league");

        await proLeagueManager.Vote(context, applicantId, context.User.Id, approve, region, userRepository);

        await context.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Thanks for your insight!")
                .AsEphemeral());
    }

    public static DiscordComponent[] ApplicationButtons(ulong discordId, DiscordEngine discordEngine)
    {
        return
        [
            new DiscordButtonComponent(
                ButtonStyle.Primary,
                discordEngine.ButtonCommands.BuildButtonId(nameof(ProApplicationVote), discordId, 1),
                "Approve",
                false,
                new DiscordComponentEmoji(DiscordEmoji.FromName(discordEngine.Client, ":heavy_check_mark:"))),

            new DiscordButtonComponent(
                ButtonStyle.Danger,
                discordEngine.ButtonCommands.BuildButtonId(nameof(ProApplicationVote), discordId.ToString(), 0),
                "Decline",
                false,
                new DiscordComponentEmoji(DiscordEmoji.FromName(discordEngine.Client, ":heavy_multiplication_x:"))),
        ];
    }
}
