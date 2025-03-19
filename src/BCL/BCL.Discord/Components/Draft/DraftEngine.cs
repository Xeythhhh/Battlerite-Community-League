using System.Diagnostics;

using BCL.Core;
using BCL.Discord.Bot;
using BCL.Discord.Extensions;
using BCL.Domain.Dtos;
using BCL.Domain.Entities.Abstract;
using BCL.Domain.Entities.Matches;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

#pragma warning disable CS4014

namespace BCL.Discord.Components.Draft;
public class DraftEngine(
    IChampionRepository championRepository,
    IMapRepository mapRepository,
    MatchManager matchManager,
    DiscordEngine discordEngine)
{
    public async Task Draft(InteractionContext context, string tokenId, DraftTokenType tokenType, DraftAction action)
    {
        await context.CreateResponseAsync("Processing draft action...");
        DiscordMatch discordMatch = matchManager.GetMatch(context.User)!;
        await discordMatch.Gate.WaitAsync(5000);

        TokenEntity? entity = tokenType switch
        {
            DraftTokenType.Champion => await championRepository.GetByIdAsync(tokenId),
            DraftTokenType.Map => await mapRepository.GetByIdAsync(tokenId),
            _ => throw new UnreachableException()
        };

        if (!await CanDraft(context, discordMatch, entity, action)) { discordMatch.Gate.Release(); return; }
        try
        {
            discordMatch.DraftTimes.Add(new DraftTime(context.User.Id, discordMatch.DraftActionStopwatch.Elapsed));
            await HandleDraftAction(context, discordMatch, entity!);
        }
        catch (Exception e)
        {
            discordEngine.Log(e);
        }

        discordMatch.Gate.Release();
        context.DeleteResponseAsync(1);
    }

    private static async Task HandleDraftAction(BaseContext context, DiscordMatch discordMatch, TokenEntity entity)
    {
        Match.Side side = discordMatch.Match.GetSide(context.User);
        DraftAction action = discordMatch.CurrentStep!.Action;

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (side)
        {
            case Match.Side.Team1 when action is DraftAction.Pick:
                discordMatch.CurrentStep.TokenId1 = entity.Id;
                discordMatch.Team1.Pick(entity, discordMatch.CurrentStep);
                break;
            case Match.Side.Team1 when action is DraftAction.Ban or DraftAction.GlobalBan:
                discordMatch.CurrentStep.TokenId1 = entity.Id;
                discordMatch.Team1.Ban(entity, discordMatch.CurrentStep);
                break;

            case Match.Side.Team2 when action is DraftAction.Pick:
                discordMatch.CurrentStep.TokenId2 = entity.Id;
                discordMatch.Team2.Pick(entity, discordMatch.CurrentStep);
                break;
            case Match.Side.Team2 when action is DraftAction.Ban or DraftAction.GlobalBan:
                discordMatch.CurrentStep.TokenId2 = entity.Id;
                discordMatch.Team2.Ban(entity, discordMatch.CurrentStep);
                break;

            default: throw new UnreachableException();
        }

        if (discordMatch.Match.Draft.DraftType is DraftType.Sequential)
        {
            bool isLastPick = discordMatch.CurrentStep.IsConcluded && (discordMatch.NextStep?.IsLastPick ?? false);
            if (isLastPick) discordMatch.Match.Draft.RemainingActions = 0;
            else discordMatch.Match.Draft.RemainingActions--;

            if (discordMatch.Match.Draft.RemainingActions == 0)
            {
                discordMatch.Match.Draft.IsTeam1Turn = !discordMatch.Match.Draft.IsTeam1Turn;

                DiscordChannel channel = discordMatch.Match.Draft.IsTeam1Turn
                    ? discordMatch.Team1.TextChannel
                    : discordMatch.Team2.TextChannel;

                Domain.Entities.Users.User nextTurnCaptain = discordMatch.Match.Draft.IsTeam1Turn
                    ? discordMatch.Team1.Captain
                    : discordMatch.Team2.Captain;

                await channel.SendMessageAsync($"{nextTurnCaptain.Mention} it's your turn to draft!");
                discordMatch.Match.Draft.RemainingActions = isLastPick
                    ? 1
                    : CoreConfig.Draft.Sequential.ActionsPerTurn;
            }
        }

        await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Action: `{discordMatch.CurrentStep.Action}` Token: `{entity.Name}`"));

        if (discordMatch.CurrentStep.IsConcluded)
        {
            discordMatch.Advance();
            if (discordMatch.CurrentStep?.IsLastPick ?? false)
            {
                discordMatch.Team1.TextChannel.SendMessageAsync($"{string.Join(" ", discordMatch.Team1.Users.Select(u => u.Mention))} please join the lobby!");
                discordMatch.Team2.TextChannel.SendMessageAsync($"{string.Join(" ", discordMatch.Team2.Users.Select(u => u.Mention))} please join the lobby!");
            }
            else if (discordMatch.CurrentStep is null)
            {
                discordMatch.PingOnUpdate = true;
                discordMatch.DraftFinishedAt = DateTime.UtcNow;
                discordMatch.UpdateChannelOverrides();
            }
        }

        discordMatch.Update();
    }

    #region Validation

    private static async Task<bool> CanDraft(BaseContext context, DiscordMatch discordMatch, TokenEntity? entity, DraftAction action)
    {
        return await IsReadyCheckPassed(context, discordMatch)
               && await IsValidEntity(context, entity)
               && await IsDraftInProgress(context, discordMatch)
               && await IsExpectedTurn(context, discordMatch)
               && await IsTokenAvalable(context, discordMatch, entity!, action)
               && await IsExpectedDraftAction(context, discordMatch.CurrentStep!, action);
    }

    private static async Task<bool> IsReadyCheckPassed(BaseContext context, DiscordMatch discordMatch)
    {
        if (!discordMatch.Ready)
        {
            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Waiting for ReadyCheck."));
        }

        return discordMatch.Ready;
    }
    private static async Task<bool> IsValidEntity(BaseContext context, TokenEntity? entity)
    {
        bool isValidEntity = entity is not null;

        if (!isValidEntity)
        {
            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Token was null."));
        }

        return isValidEntity;
    }
    private static async Task<bool> IsDraftInProgress(BaseContext context, DiscordMatch discordMatch)
    {
        bool inProgress = !discordMatch.Match.Draft.IsFinished;

        if (!inProgress)
        {
            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Draft is finished."));
        }

        return inProgress;
    }
    private static async Task<bool> IsExpectedTurn(BaseContext context, DiscordMatch discordMatch)
    {
        Match.Side side = discordMatch.Match.GetSide(context.User);
        // ReSharper disable once InvertIf
        if (discordMatch.Match.Draft.DraftType is DraftType.Sequential &&

            ((discordMatch.Match.Draft.IsTeam1Turn && side is Match.Side.Team2) ||
             (!discordMatch.Match.Draft.IsTeam1Turn && side is Match.Side.Team1)))
        {
            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("It is not your turn to draft!"));
            return false;
        }

        return true;
    }
    private static async Task<bool> IsTokenAvalable(BaseContext context, DiscordMatch discordMatch, TokenEntity entity, DraftAction action)
    {
        if (action is DraftAction.MapBan or DraftAction.MapPick) throw new NotImplementedException();

        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        List<TokenEntity> unavailable = discordMatch.Match.GetSide(context.User) switch
        {
            Match.Side.Team1 =>
                //When action is Pick
                discordMatch.Team2.Bans.Where(_ => action is DraftAction.Pick)
                    .Concat(discordMatch.Team1.Picks.Where(_ => action is DraftAction.Pick))

                    //When action is Ban
                    .Concat(discordMatch.Team1.Bans.Where(_ => action is DraftAction.GlobalBan or DraftAction.Ban))
                    .Concat(discordMatch.Team2.Picks.Where(_ => action is DraftAction.GlobalBan or DraftAction.Ban))

                    //Global bans
                    .Concat(discordMatch.Team1.Bans.Where(s => s.Action is DraftAction.GlobalBan))
                    .Concat(discordMatch.Team2.Bans.Where(s => s.Action is DraftAction.GlobalBan))

                    .Select(s => s.Entity).DistinctBy(c => c.Id).ToList(),

            Match.Side.Team2 =>
                //When action is Pick
                discordMatch.Team1.Bans.Where(_ => action is DraftAction.Pick)
                    .Concat(discordMatch.Team2.Picks.Where(_ => action is DraftAction.Pick))

                    //When action is Ban
                    .Concat(discordMatch.Team2.Bans.Where(_ => action is DraftAction.GlobalBan or DraftAction.Ban))
                    .Concat(discordMatch.Team1.Picks.Where(_ => action is DraftAction.GlobalBan or DraftAction.Ban))

                    //Global bans
                    .Concat(discordMatch.Team2.Bans.Where(s => s.Action is DraftAction.GlobalBan))
                    .Concat(discordMatch.Team1.Bans.Where(s => s.Action is DraftAction.GlobalBan))

                    .Select(s => s.Entity).DistinctBy(c => c.Id).ToList(),

            _ => throw new UnreachableException()
        };

        if (discordMatch.Match.League is League.Standard && entity.StandardBanned) unavailable.Add(entity);

        bool isTokenBanned = unavailable.Any(c => c.Id == entity.Id);
        if (isTokenBanned) await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"`{entity.Name}` is not available!"));
        return !isTokenBanned;
    }
    private static async Task<bool> IsExpectedDraftAction(BaseContext context, DraftStep currentStep, DraftAction userAction)
    {
        DraftAction expectedAction = currentStep.Action == DraftAction.GlobalBan ? DraftAction.Ban : currentStep.Action; //GlobalBan is treated as a regular ban for simplicity
        if (expectedAction == userAction) return true;
        await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Invalid operation, expected `{expectedAction}`"));
        return false;
    }

    #endregion
}
