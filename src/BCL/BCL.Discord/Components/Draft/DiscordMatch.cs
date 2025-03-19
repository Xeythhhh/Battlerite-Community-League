using System.Diagnostics;

using BCL.Core;
using BCL.Core.Services.Args;
using BCL.Discord.Bot;
using BCL.Discord.Commands.ButtonCommands;
using BCL.Discord.Extensions;
using BCL.Domain.Dtos;
using BCL.Domain.Entities.Matches;
using BCL.Domain.Entities.Queue;
using BCL.Domain.Enums;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

#pragma warning disable CS4014
#pragma warning disable CS8618

namespace BCL.Discord.Components.Draft;
public partial class DiscordMatch(MatchStartedEventArgs args, DiscordRole inMatchRole, DiscordEngine discordEngine/*, IBankService bank*/)
{
    //private readonly IBankService _bank;
    private readonly Map _map = args.Map;
    private readonly List<string> _format = args.Format;

    // ReSharper disable InconsistentNaming
    internal enum _VoteType { Report, Bet, Prediction, TimedOut }
    internal record _Vote(ulong DiscordId, MatchOutcome Outcome, _VoteType Type, double Bet = 0);
    // ReSharper restore InconsistentNaming
    private readonly List<_Vote> _reports = [];
    private readonly List<_Vote> _predictions = [];

    public Match Match { get; set; } = args.Match;
    public DraftStep? CurrentStep => Match.Draft.CurrentStep;
    public DraftStep? NextStep => Match.Draft.NextStep;

    #region Snowflake objects

    public DiscordMessage QueueChannelMessage { get; set; }
    public DiscordMessage AdminMessage { get; set; }
    public DiscordRole MatchRole { get; set; } = inMatchRole;
    public IEnumerable<DiscordMessage> Messages => new List<DiscordMessage> { QueueChannelMessage, Team1.DiscordMessage, Team2.DiscordMessage };
    public DiscordChannel[] Channels
    {
        get
        {
            DiscordChannel?[] discordChannels = [Team1.TextChannel, Team2.TextChannel, Team1.VoiceChannel, Team2.VoiceChannel];
            return discordChannels.Where(c => c is not null).ToArray()!;
        }
    }

    #endregion

    #region Teams

    public Team Team1 { get; set; } = args.Team1;
    public Team Team2 { get; set; } = args.Team2;
    public IEnumerable<Team.Step> Picks => Team1.Picks.UnionBy(Team2.Picks, s => s.Entity.Id);

    #endregion

    #region Timestamps

    public List<DraftTime> DraftTimes { get; set; } = [];
    public Stopwatch DraftActionStopwatch { get; set; } = new();
    public DateTime DraftStartedAt { get; set; }
    public DateTime? DraftFinishedAt { get; set; }
    public TimeSpan ReadyCheckDuration { get; set; }

    #endregion

    #region Meta

    public SemaphoreSlim Gate { get; set; } = new(1, 1);
    public bool PingOnUpdate { get; set; }
    public bool Ready =>
        ReadyCheckEntries.Contains(Team1.Captain.DiscordId) &&
        ReadyCheckEntries.Contains(Team2.Captain.DiscordId);
    public List<ulong> ReadyCheckEntries { get; set; } = [];

    #endregion

    public async Task Initialize()
    {
        Match.Team1ChannelId = Team1.TextChannel.Id;
        Match.Team2ChannelId = Team2.TextChannel.Id;
        DraftStartedAt = DateTime.UtcNow;
        DraftActionStopwatch.Reset();

        QueueChannelMessage = await GetMessage(Match.Side.None, $"Draft Started {MatchRole.Mention}!")
            .WithAllowedMention(new RoleMention(MatchRole))
            .SendAsync(discordEngine.QueueChannel);

        Team1.DiscordMessage = await GetMessage(Match.Side.Team1, $"It's your turn to draft {Team1.Captain.Mention}!").SendAsync(Team1.TextChannel);
        Team2.DiscordMessage = await GetMessage(Match.Side.Team2, Match.Draft.DraftType is DraftType.Sequential ? "Good luck!" : $"It's your turn to draft {Team1.Captain.Mention}!").SendAsync(Team2.TextChannel);
    }
    public void Start()
    {
        ReadyCheckDuration = DateTime.UtcNow - DraftStartedAt;
        DraftActionStopwatch.Start();
        Match.Draft.Steps[0].Start();
        Update();
    }
    public async Task Update(bool isDraftAction = true)
    {
        if (Match.Draft.IsFinished && DraftFinishedAt is null) DraftFinishedAt = DateTime.UtcNow;

        if ((isDraftAction && Match.Draft.DraftType == DraftType.Sequential) || (CurrentStep?.IsNew ?? false)) DraftActionStopwatch.Restart();

        #region Queue

        DiscordChannel queueChannel = discordEngine.QueueChannel;
        try
        {
            if (PingOnUpdate)
            {
                QueueChannelMessage.DeleteAsync();
                QueueChannelMessage = await GetMessage(Match.Side.None, $"Draft Finished {MatchRole.Mention}!")
                    .WithAllowedMention(new RoleMention(MatchRole))
                    .SendAsync(queueChannel);

                PingOnUpdate = false;
            }
            else
            {
                await QueueChannelMessage.ModifyAsync(m =>
            {
                m.Embed = GetEmbed();
                m.AddComponents(GetComponents());
            });
            }
        }
        catch (Exception e)
        {
            if (e is NotFoundException or ServerErrorException)
                QueueChannelMessage = await GetMessage(Match.Side.None, MatchRole.Mention).SendAsync(queueChannel);
            else discordEngine.Log(e);
        }

        #endregion

        #region Team 1

        try
        {
            if (!isDraftAction)
            {
                await Team1.DiscordMessage.ModifyAsync(m =>
            {
                m.Embed = GetEmbed(Match.Side.Team1);
                m.AddComponents(GetComponents());
            });
            }
            else
            {
                Team1.DiscordMessage.DeleteAsync();
                Team1.DiscordMessage = await GetMessage(Match.Side.Team1).SendAsync(Team1.TextChannel);
            }
        }
        catch (Exception e)
        {
            if (e is NotFoundException or ServerErrorException or NullReferenceException)
                Team1.DiscordMessage = await GetMessage(Match.Side.Team1).SendAsync(Team1.TextChannel);
            else discordEngine.Log(e);
        }

        #endregion

        #region Team 2

        try
        {
            if (!isDraftAction)
            {
                await Team2.DiscordMessage.ModifyAsync(m =>
            {
                m.Embed = GetEmbed(Match.Side.Team2);
                m.AddComponents(GetComponents());
            });
            }
            else
            {
                Team2.DiscordMessage.DeleteAsync();
                Team2.DiscordMessage = await GetMessage(Match.Side.Team2).SendAsync(Team2.TextChannel);
            }
        }
        catch (Exception e)
        {
            if (e is NotFoundException or ServerErrorException or NullReferenceException)
                Team2.DiscordMessage = await GetMessage(Match.Side.Team2).SendAsync(Team2.TextChannel);
            else discordEngine.Log(e);
        }

        #endregion
    }
    public void Advance()
    {
        DraftStep? next = NextStep;
        CurrentStep!.Finish();
        next?.Start();
    }
    private DiscordMessageBuilder GetMessage(Match.Side side = Match.Side.None, string? message = null)
    {
        DiscordMessageBuilder builder = new DiscordMessageBuilder()
            .AddEmbed(GetEmbed(side))
            .AddComponents(GetComponents());
        if (message != null) builder.WithContent(message);
        return builder;
    }
    public async Task UpdateChannelOverrides()
    {
        if (Team1.VoiceChannel is not null)
            await Team1.VoiceChannel.AddOverwriteAsync(discordEngine.Guild.EveryoneRole, Permissions.AccessChannels);

        if (Team2.VoiceChannel is not null)
            await Team2.VoiceChannel.AddOverwriteAsync(discordEngine.Guild.EveryoneRole, Permissions.AccessChannels);
    }

    public bool CanBet => DraftFinishedAt is null || DateTime.UtcNow - DraftFinishedAt <= TimeSpan.FromMinutes(5);
    public async Task Vote(DiscordInteraction interaction, MatchOutcome outcome)
    {
        DiscordUser discordUser = interaction.User;

        #region Logic

        _Vote? previousVote = _reports.SingleOrDefault(r => r.DiscordId == discordUser.Id) ??
                              _predictions.SingleOrDefault(r => r.DiscordId == discordUser.Id);

        if (previousVote is not null)
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (previousVote.Type)
            {
                case _VoteType.Report:
                    _reports.Remove(previousVote);
                    break;

                case _VoteType.Bet:

                    if (CanBet)
                    {
                        //_bank.Refund(Match.Id, interaction.User.Id);
                        _predictions.Remove(previousVote);
                    }
                    break;

                case _VoteType.Prediction:
                    _predictions.Remove(previousVote);
                    break;

                default: throw new UnreachableException();
            }
        }

        _VoteType voteType = true switch
        {
            _ when Match.DiscordUserIds.Contains(interaction.User.Id) => _VoteType.Report,
            _ when previousVote?.Type is _VoteType.Bet && !CanBet => _VoteType.TimedOut,
            //_ when CanBet => _VoteType.Bet,
            _ => _VoteType.Prediction
        };

        switch (voteType)
        {
            case _VoteType.Report:
                _reports.Add(new _Vote(discordUser.Id, outcome, _VoteType.Report));
                break;

            //case _VoteType.Bet:
            //    var response = _bank.Bet(Match.Id, interaction.User.Id, outcome);
            //    if (response.Status == BetStatus.Declined)
            //    {
            //        await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
            //            .WithContent($"Bet failed, registered prediction. {response.Message}"));
            //        _predictions.Add(new _Vote(discordUser.Id, outcome, _VoteType.Prediction));

            //        return;
            //    }
            //    _predictions.Add(new _Vote(discordUser.Id, outcome, _VoteType.Bet, response.Amount));
            //    break;

            case _VoteType.Prediction:
                _predictions.Add(new _Vote(discordUser.Id, outcome, _VoteType.Prediction));
                break;

            case _VoteType.TimedOut:
                await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Bet period is over."));
                return;

            default: throw new UnreachableException();
        }

        #endregion

        Match.Outcome = true switch
        {
            _ when _reports.Count(r => r.Outcome == MatchOutcome.Team1) >= CoreConfig.Draft.RequiredReports => MatchOutcome.Team1,
            _ when _reports.Count(r => r.Outcome == MatchOutcome.Team2) >= CoreConfig.Draft.RequiredReports => MatchOutcome.Team2,
            _ when _reports.Count(r => r.Outcome == MatchOutcome.Canceled) >= CoreConfig.Draft.RequiredReports => MatchOutcome.Canceled,
            _ => MatchOutcome.InProgress
        };

        if (Match.Outcome is MatchOutcome.InProgress)
        {
            try
            {
                // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
                string action = voteType switch
                {
                    _VoteType.Report => "Outcome report",
                    _VoteType.Bet => "Bet",
                    _VoteType.Prediction => "Prediction",
                    _ => throw new UnreachableException()
                };

                interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"**{action}** updated {discordUser.Mention}")
                    .AddMention(new UserMention(discordUser)));
            }
            catch (NotFoundException) { /* ignored */ }

            Update(false);
        }
        else
        {
            Task.Run(() => discordEngine.FinishMatch(this, Match.Outcome is MatchOutcome.Canceled ? "Dropped by user votes." : null));
        }

        interaction.DeleteOriginalResponseAsync(5);
    }

    #region Components

    private DiscordComponent[] GetComponents()
    {
        List<DiscordComponent> components =
        [
            new DiscordButtonComponent(
                ButtonStyle.Primary,
                discordEngine.ButtonCommands.BuildButtonId(nameof(MatchButtons.Vote), Match.Id, MatchOutcome.Team1),
                "Team 1",
                false,
                new DiscordComponentEmoji(DiscordEmoji.FromName(discordEngine.Client, ":trophy:"))),
            new DiscordButtonComponent(
                ButtonStyle.Primary,
                discordEngine.ButtonCommands.BuildButtonId(nameof(MatchButtons.Vote), Match.Id, MatchOutcome.Team2),
                "Team 2",
                false,
                new DiscordComponentEmoji(DiscordEmoji.FromName(discordEngine.Client, ":trophy:"))),
            new DiscordButtonComponent(
                ButtonStyle.Danger,
                discordEngine.ButtonCommands.BuildButtonId(nameof(MatchButtons.Vote), Match.Id, MatchOutcome.Canceled),
                "Drop",
                false,
                new DiscordComponentEmoji(DiscordEmoji.FromName(discordEngine.Client, ":shit:")))
        ];

        if (ReadyCheckEntries.Count != Match.DiscordUserIds.Count())
            components.Add(ReadyCheck);

        return [.. components];
    }
    private DiscordComponent ReadyCheck =>
        new DiscordButtonComponent(
            ButtonStyle.Success,
            discordEngine.ButtonCommands.BuildButtonId(nameof(MatchButtons.ReadyCheck), Match.Id),
            "ReadyCheck",
            false,
            new DiscordComponentEmoji(DiscordEmoji.FromName(discordEngine.Client, ":white_check_mark:")));

    #endregion
}
