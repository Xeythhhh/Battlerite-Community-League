using BCL.Discord.Attributes.Matches;
using BCL.Discord.Components;
using BCL.Discord.Components.Draft;
using BCL.Discord.OptionProviders;
using BCL.Domain.Entities.Matches;
using BCL.Domain.Enums;

using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;

#pragma warning disable CS4014

namespace BCL.Discord.Commands.SlashCommands.Draft;
[SlashModuleLifespan(SlashModuleLifespan.Scoped)]
public partial class DraftCommands(MatchManager matchManager) : ApplicationCommandModule
{
    [SlashCommand_InMatch]
    [SlashCommand_TeamCaptain]
    [SlashCommand_TeamChannel]
    [SlashCommandGroup("Draft", "Pick / Ban a champion.")]
    [SlashModuleLifespan(SlashModuleLifespan.Scoped)]
    public class Draft(DraftEngine draftEngine, MatchManager matchManager) : ApplicationCommandModule
    {
        #region Ban

        [SlashCommand("BanMelee", "Ban a melee champion.")]
        public async Task BanMelee(InteractionContext context,
            [ChoiceProvider(typeof(MeleeChoiceProvider))]
            [Option("Champion", "Melee champion you intend to ban")]string championId)
            => await draftEngine.Draft(context, championId, DraftTokenType.Champion, DraftAction.Ban);

        [SlashCommand("BanRanged", "Ban a ranged champion.")]
        public async Task BanRanged(InteractionContext context,
            [ChoiceProvider(typeof(RangedChoiceProvider))]
            [Option("Champion", "Ranged champion you intend to ban")]string championId)
            => await draftEngine.Draft(context, championId, DraftTokenType.Champion, DraftAction.Ban);

        [SlashCommand("BanSupport", "Ban a support champion.")]
        public async Task BanSupport(InteractionContext context,
            [ChoiceProvider(typeof(SupportChoiceProvider))]
            [Option("Champion", "Support champion you intend to ban")]string championId)
            => await draftEngine.Draft(context, championId, DraftTokenType.Champion, DraftAction.Ban);

        #endregion

        #region Pick

        [SlashCommand("PickMelee", "Pick a melee champion.")]
        public async Task PickMelee(InteractionContext context,
            [ChoiceProvider(typeof(MeleeChoiceProvider))]
            [Option("Champion", "Melee champion you intend to pick")]string championId)
            => await draftEngine.Draft(context, championId, DraftTokenType.Champion, DraftAction.Pick);

        [SlashCommand("PickRanged", "Pick a ranged champion.")]
        public async Task PickRanged(InteractionContext context,
            [ChoiceProvider(typeof(RangedChoiceProvider))]
            [Option("Champion", "Ranged champion you intend to pick")]string championId)
            => await draftEngine.Draft(context, championId, DraftTokenType.Champion, DraftAction.Pick);

        [SlashCommand("PickSupport", "Pick a support champion.")]
        public async Task PickSupport(InteractionContext context,
            [ChoiceProvider(typeof(SupportChoiceProvider))]
            [Option("Champion", "Support champion you intend to pick")]string championId)
            => await draftEngine.Draft(context, championId, DraftTokenType.Champion, DraftAction.Pick);

        #endregion

        [SlashCommand("Captain", "Delegate captain to a teammate")]
        public async Task Captain(InteractionContext context,
            [Option("DiscordUser", "Teammate you wanna delegate captain to")] DiscordUser newCaptain)
        {
            await context.CreateResponseAsync("Delegating captain...");
            if (context.User.Id == newCaptain.Id)
            {
                context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("bruh")
                    .AddMention(new UserMention(context.User)));
                return;
            }

            DiscordMatch discordMatch = matchManager.GetMatch(context.User)!;
            if (discordMatch.DraftFinishedAt is not null && discordMatch.Match.Draft.IsFinished)
            {
                context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Can not change captain once draft is finished.")
                    .AddMention(new UserMention(context.User)));
                return;
            }

            Match.Side side = discordMatch.Match.GetSide(context.User);
            if (side != discordMatch.Match.GetSide(newCaptain))
            {
                context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"{newCaptain.Mention} is not on your team. {context.User.Mention}")
                    .AddMention(new UserMention(context.User)));
                return;
            }

            #region Confirmation

            DSharpPlus.Interactivity.InteractivityExtension interactivity = context.Client.GetInteractivity();

            DiscordChannel teamChannel = side switch
            {
                Match.Side.Team1 => discordMatch.Team1.TextChannel,
                Match.Side.Team2 => discordMatch.Team2.TextChannel,
                Match.Side.None => throw new ArgumentException("Not in a match."),
                _ => throw new ArgumentOutOfRangeException(nameof(side), side, "Team side out of range")
            };

            DiscordMessage confirmationPrompt = await teamChannel.SendMessageAsync(new DiscordMessageBuilder()
                .WithContent($"""
                              {context.User.Mention} wants you to draft.
                              React with :white_check_mark: to accept or :x: to decline {newCaptain.Mention}.
                              """));

            DiscordEmoji confirmEmoji = DiscordEmoji.FromName(context.Client, ":white_check_mark:");
            DiscordEmoji declineEmoji = DiscordEmoji.FromName(context.Client, ":x:");

            confirmationPrompt.CreateReactionAsync(confirmEmoji);
            confirmationPrompt.CreateReactionAsync(declineEmoji);

            bool validReaction = false;
            while (!validReaction)
            {
                DSharpPlus.Interactivity.InteractivityResult<DSharpPlus.EventArgs.MessageReactionAddEventArgs> response = await interactivity.WaitForReactionAsync(r =>
                    r.Message.Id == confirmationPrompt.Id
                     && r.User.Id == newCaptain.Id, TimeSpan.FromMinutes(1));

                if (response.TimedOut || response.Result.Emoji == declineEmoji)
                {
                    Domain.Entities.Users.User captain = side == Match.Side.Team1
                        ? discordMatch.Team1.Captain
                        : discordMatch.Team2.Captain;

                    confirmationPrompt.ModifyAsync($":x: Declined{(response.TimedOut ? " (Timed out...)" : ".")}");
                    teamChannel.SendMessageAsync($"{captain.Mention} you are the team captain!");
                    return;
                }
                if (response.Result.Emoji == confirmEmoji) validReaction = true;
            }

            #endregion

            await discordMatch.Gate.WaitAsync(5000);
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (side)
            {
                //invalidate if multiple requests have been sent and one was already accepted
                case Match.Side.Team1 when discordMatch.Team1.Captain.DiscordId != context.User.Id:
                case Match.Side.Team2 when discordMatch.Team2.Captain.DiscordId != context.User.Id:
                    confirmationPrompt.ModifyAsync($"{newCaptain.Mention} another request has already been accepted.⚠️");
                    discordMatch.Gate.Release();
                    return;
            }

            discordMatch.MakeCaptain(newCaptain);
            confirmationPrompt.ModifyAsync(":white_check_mark: Accepted.");
            discordMatch.Gate.Release();
        }
    }
}
