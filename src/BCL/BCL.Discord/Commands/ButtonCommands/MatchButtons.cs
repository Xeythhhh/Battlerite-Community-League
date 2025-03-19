using BCL.Discord.Components;
using BCL.Discord.Extensions;
using BCL.Domain.Entities.Matches;
using BCL.Domain.Enums;

using DSharpPlus;
using DSharpPlus.ButtonCommands;
using DSharpPlus.Entities;

#pragma warning disable CS4014
namespace BCL.Discord.Commands.ButtonCommands;

public class MatchButtons(MatchManager matchManager) : ButtonCommandModule
{
    [ButtonCommand("Vote")]
    public async Task Vote(ButtonContext context, Ulid matchId, MatchOutcome outcome)
    {
        await context.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .WithContent($"Handling **Match Interaction** for {context.User.Mention}...")
                .AsEphemeral());

        Components.Draft.DiscordMatch? discordMatch = matchManager.GetMatch(matchId);
        if (discordMatch is null)
        {
            await context.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"An __active__ match with id `{matchId}` was __not found__."));

            context.Interaction.DeleteOriginalResponseAsync(5);
            return;
        }

        await discordMatch.Vote(context.Interaction, outcome);
    }

    [ButtonCommand("ReadyCheck")]
    public async Task ReadyCheck(ButtonContext context, Ulid matchId)
    {
        await context.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .WithContent($"Handling **ReadyCheck Interaction** for {context.User.Mention}...")
                .AsEphemeral());

        Components.Draft.DiscordMatch? discordMatch = matchManager.GetMatch(matchId);
        if (discordMatch is null)
        {
            await context.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"An __active__ match with id `{matchId}` was __not found__."));

            context.Interaction.DeleteOriginalResponseAsync(5);
            return;
        }

        Match.Side side = discordMatch.Match.GetSide(context.User);
        if (side is Match.Side.None)
        {
            await context.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"You are not in match `{matchId}`."));
            return;
        }

        if (discordMatch.ReadyCheckEntries.Contains(context.User.Id))
        {
            await context.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"You have already passed the ReadyCheck for `{matchId}`."));
            return;
        }

        await discordMatch.Gate.WaitAsync(5000);
        discordMatch.ReadyCheckEntries.Add(context.User.Id);

        await context.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
            .WithContent(":ok_hand:"));

        if (discordMatch.Ready) discordMatch.Start();
        else discordMatch.Update(false);

        discordMatch.Gate.Release();

        await context.Interaction.DeleteOriginalResponseAsync(1);
    }
}
