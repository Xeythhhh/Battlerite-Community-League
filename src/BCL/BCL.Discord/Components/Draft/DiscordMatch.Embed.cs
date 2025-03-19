using System.Diagnostics;

using BCL.Common.Extensions;
using BCL.Core;
using BCL.Discord.Components.Dashboards;
using BCL.Discord.Extensions;
using BCL.Discord.Utils;
using BCL.Domain;
using BCL.Domain.Dtos;
using BCL.Domain.Entities.Matches;
using BCL.Domain.Entities.Queue;
using BCL.Domain.Enums;

using DSharpPlus.Entities;

using Humanizer;
using Humanizer.Localisation;

namespace BCL.Discord.Components.Draft;
public partial class DiscordMatch
{
    /// <summary>
    /// If cast to int to index embed fields do -1 because Description is not a field
    /// </summary>
    public enum EmbedStructure
    {
        Description,
        Team1,
        Team1Bans,
        Team1Picks,
        Team2,
        Team2Bans,
        Team2Picks,
        Restrictions,
        Outcome,
        Predictions,
        TimeStamps
    }

    #region Embed fields

    // ReSharper disable InconsistentNaming
    public string Embed_Description { get; set; } = "No Info";
    public string Embed_Team1 => GetTeamField(Team1);
    public string Embed_Team1Bans => FormatDraftSteps(Team1.Bans, CurrentStep?.Action is DraftAction.Ban or DraftAction.GlobalBan && Match.Draft.IsTeam1Turn, DraftAction.Ban);
    public string Embed_Team1Picks => FormatDraftSteps(Team1.Picks, CurrentStep?.Action is DraftAction.Pick && Match.Draft.IsTeam1Turn, DraftAction.Pick);
    public string Embed_Team2 => GetTeamField(Team2);
    public string Embed_Team2Bans => FormatDraftSteps(Team2.Bans, CurrentStep?.Action is DraftAction.Ban or DraftAction.GlobalBan && !Match.Draft.IsTeam1Turn, DraftAction.Ban);
    public string Embed_Team2Picks => FormatDraftSteps(Team2.Picks, CurrentStep?.Action is DraftAction.Pick && !Match.Draft.IsTeam1Turn, DraftAction.Pick);
    public string Embed_Restrictions
    {
        get
        {
            List<Team.Step> restricted = [.. Picks.Where(s => s.Entity is Champion { Restricted: true }).OrderBy(s => s.Entity.Name)];
            if (restricted.Count == 0) return "No Restrictions";
            int length = restricted.Max(s => s.Entity.Name.Length);

            string content = """
                ```ANSI
                """;

            foreach (Team.Step? step in restricted)
                content = $"{content}\n{$"{ANSIColors.Reset}{ANSIColors.Underline}{ANSIColors.White}{ANSIColors.Background.Black} {step.Entity.Name} {new string(' ', 37 - step.Entity.Name.Length)}{ANSIColors.Reset}{(step.Entity.Disabled ? $"{ANSIColors.Red}⚠Disabled⚠" : FormatRestrictions(((Champion)step.Entity).Restrictions))}"}";

            content += "```\n__**Note**__: `Restrictions may be ignored if all players agree`";

            return content;

            static string FormatRestrictions(string? input)
            {
                if (input is null) return string.Empty;

                IEnumerable<string> restrictions = input.Split("\n")
                    .Select(s =>
                    {
                        string[] values = s.Split(";");
                        string decorator = values.Length == 2
                            ? QueueTracker.GetAnsiColor(values[1])
                            : ANSIColors.Red;

                        return $"{ANSIColors.Background.Yellow}{ANSIColors.White}>  {ANSIColors.Reset}{ANSIColors.Background.Black}{decorator}{values[0]}{new string(' ', 36 - values[0].Length)}";
                    });

                return restrictions.Aggregate("", (current, next) => $"{current}\n{next}");
            }
        }
    }
    public string Embed_Outcome => _reports.Count == 0 ? "No Reports" : FormatVotes(_reports, true);
    public string Embed_Predictions => _predictions.Any(p => p.Outcome is not MatchOutcome.Canceled) ? FormatVotes(_predictions) : "No Predictions";
    public string Embed_TimeStamps
    {
        get
        {
            string value = $"""
                         Draft Started {DraftStartedAt.DiscordTime(DiscordTimeFlag.R)}
                         {string.Join('\n', DraftTimes.GroupBy(dt => dt.DiscordId).Select(g => $"<@{g.Key}>: {new TimeSpan(g.Sum(x => x.Duration.Ticks)).Humanize(precision: 3)}"))}
                         """;
            value += DraftFinishedAt is not null && Match.Draft.IsFinished
                ? $"\nDraft Finished {DraftFinishedAt.Value.DiscordTime(DiscordTimeFlag.R)}\nDuration: {(DraftFinishedAt - DraftStartedAt).Value.Humanize(precision: 2, minUnit: TimeUnit.Second)}"
                : Ready
                    ? $"\nCurrent {CurrentStep!.Action} {(DateTime.UtcNow - DraftActionStopwatch.Elapsed).DiscordTime(DiscordTimeFlag.R)}"
                    : $"\nWaiting for players ({ReadyCheckEntries.Count} / {Match.DiscordUserIds.Count()})";
            return value;
        }
    }
    public string Embed_Footer { get; set; } = "No Info";
    // ReSharper restore InconsistentNaming

    #endregion

    public DiscordEmbed GetEmbed(Match.Side side = Match.Side.None) => GetEmbedBuilder(side).Build();
    public DiscordEmbedBuilder GetEmbedBuilder(Match.Side side = Match.Side.None)
    {
        UpdateDescription();

        DiscordColor color = Match.Draft.IsFinished switch
        {
            true => DiscordColor.Aquamarine,
            false when !Ready => DiscordColor.Orange,
            _ => DiscordColor.SpringGreen
        };

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithAuthor(Match.Id.ToString(), null, discordEngine.Guild.IconUrl)
            .WithColor(color)
            .WithDescription(Embed_Description)
            .AddField($"{GetTeamElo(Team1)} {Team1.Name}", Embed_Team1, true)
            .AddField("Bans", Embed_Team1Bans, true)
            .AddField("Picks", Embed_Team1Picks, true)
            .AddField($"{GetTeamElo(Team2)} {Team2.Name}", Embed_Team2, true)
            .AddField("Bans", Embed_Team2Bans, true)
            .AddField("Picks", Embed_Team2Picks, true)
            .AddField("Restrictions", Embed_Restrictions)
            .AddField("Outcome", Embed_Outcome, true)
            .AddField("Predictions", Embed_Predictions, true)
            .AddField("Time Stamps", Embed_TimeStamps)
            .WithTimestamp(DateTime.UtcNow)
            .WithFooter(Embed_Footer, discordEngine.Guild.IconUrl);

        if (Match.Draft.DraftType is DraftType.Simultaneous) ScuffedLastAction(side, embed);

        return embed;

        string GetTeamElo(Team team) => Match.League switch
        {
            League.Pro => $"[{(int)team.Users.Average(u => u.Rating)}]",
            League.Standard => $"[{(int)team.Users.Average(u => u.Rating_Standard)}]",
            League.Premade3V3 => $"[{team.Rating}]",
            _ => string.Empty
        };
    }
    public DiscordEmbed ArchiveEmbed()
    {
        (DiscordColor color, string winner) = Match.Outcome switch
        {
            MatchOutcome.Team1 => (DiscordColor.Green, "Team 1"),
            MatchOutcome.Team2 => (DiscordColor.Green, "Team 2"),
            MatchOutcome.Canceled => (DiscordColor.Red, "Nobody"),
            MatchOutcome.InProgress => throw new ArgumentException("You can not archive an InProgress match", nameof(Match)),
            _ => throw new UnreachableException()
        };

        DiscordEmbedBuilder embed = GetEmbedBuilder()
            .WithColor(color)
            .WithFooter(Match.CancelReason ?? $"{winner}!", discordEngine.Guild.IconUrl);

        embed.GetField(EmbedStructure.Outcome).Value = $"""
                                                        ```ml
                                                        Winner: {winner}
                                                        Elo Shift {Math.Abs(Match.EloShift)}
                                                         ```
                                                        """;

        return embed.Build();
    }

    #region Embed field values

    private string MatchDescription
    {
        get
        {
            return $"""
                                        ```ANSI
                                        {ANSIColors.Background.Black}{ANSIColors.White}{DomainConfig.Season} {ANSIColors.Cyan}>{ANSIColors.White} {Match.League} {ANSIColors.Cyan}>{ANSIColors.White} {Match.MatchmakingLogic} {ANSIColors.Cyan}>{ANSIColors.White} {Match.Draft.DraftType}
                                        {string.Join($" {ANSIColors.Cyan}- ", _format.Select(ds => $"{ANSIColors.White}{ds}"))}
                                        {ANSIColors.White}{Match.Region} {ANSIColors.Cyan}|{ANSIColors.White} {_map.Name}```
                                        **Disclaimer**: *This embed is a work in progress, some things may not be displayed properly on your device. Please report any bugs!*
                                        """;
        }
    }

    private void UpdateDescription()
    {
        Embed_Description = string.Empty;

        //if (!Match.Draft.IsFinished) //only for debug
        if (Match.Draft.IsFinished)
        {
            string teams = $@"**__Team 1__**:{Team1.Players.Aggregate("", (c, n) => $"{c}\n{n.User.Mention}")}
*vs*
**__Team 2__**:{Team2.Players.Aggregate("", (c, n) => $"{c}\n{n.User.Mention}")}";
            Embed_Description = $"{teams}\n{MatchDescription}";

            if (Match.EloModifiers.Count != 0)
            {
                Embed_Description += $"\n{Match.EloModifiers.Aggregate("", (description, modifier) =>
                    $"{description}\n{(modifier.Factor < 1 ? "-" : "+")} {modifier.Label} | x{modifier.Factor} | {modifier.AppliesTo}{(modifier.ApplyToLossesOnly ? " | L" : string.Empty)}")}";
            }

            Embed_Footer = "Have fun!";
            return;
        }

        if (ReadyCheckEntries.Count != Match.DiscordUserIds.Count())
        {
            if (!Ready) Embed_Footer = "Waiting for players...";

            string readyCheckInfo = Match.DiscordUserIds.Aggregate(
                    "", (current, discordId) => $"{current}\n{(ReadyCheckEntries.Contains(discordId) ? ":green_circle:" : ":red_circle:")} <@{discordId}> {(Match.IsCaptain(discordId, out _) ? " - Captain" : string.Empty)}")
                .Trim();
            Embed_Description = $"{readyCheckInfo}";
        }

        if (Ready)
        {
            string mentions = string.Empty;
            switch (Match.Draft.DraftType)
            {
                case DraftType.Simultaneous:
                    if (CurrentStep!.TokenId1 == default) mentions += Team1.Captain.Mention;
                    if (CurrentStep.TokenId2 == default) mentions += Team2.Captain.Mention;
                    Embed_Footer = true switch
                    {
                        _ when CurrentStep.TokenId1 == default &&
                               CurrentStep.TokenId2 != default => $"Waiting for Team1 {CurrentStep.Action}",

                        _ when CurrentStep.TokenId1 != default &&
                               CurrentStep.TokenId2 == default => $"Waiting for Team2 {CurrentStep.Action}",

                        _ => $"Waiting for {CurrentStep.Action}s"
                    };
                    break;

                case DraftType.Sequential:
                    mentions = Match.Draft.IsTeam1Turn ? Team1.Captain.Mention : Team2.Captain.Mention;
                    Embed_Footer = Match.Draft.IsTeam1Turn
                        ? $"Waiting for Team1 {CurrentStep!.Action}"
                        : $"Waiting for Team2 {CurrentStep!.Action}";
                    break;

                default: throw new UnreachableException("Invalid DraftType");
            }
            Embed_Description += $"\n\nWaiting for {mentions} {CurrentStep.Action}";
        }

        Embed_Description += $"\n{MatchDescription}";
        Embed_Description = Embed_Description.Trim(); //kek
    }
    private static string FormatVotes(List<_Vote> collection, bool reports = false)
    {
        int team1 = collection.Count(p => p.Outcome is MatchOutcome.Team1);
        int team2 = collection.Count(p => p.Outcome is MatchOutcome.Team2);

        string dropVotes = $"Drop  : {collection.Count(p => p.Outcome is MatchOutcome.Canceled)}";
        string predictionPot = $"Pot   : {collection.Sum(p => p.Bet).ToGuildCurrencyString()}";

        return $"""
                ```ml
                Team 1: {team1}{(!reports ? $" | {(double)team1 / collection.Count:0.00%}" : string.Empty)}
                Team 2: {team2}{(!reports ? $" | {(double)team2 / collection.Count:0.00%}" : string.Empty)}
                {(reports ? dropVotes : predictionPot)}```
                """;
    }
    private string GetTeamField(Team team)
    {
        int ratingLength = Match.League is League.Pro
            ? $"{team.Users.Max(p => p.Rating)}".Length
            : $"{team.Users.Max(p => p.Rating_Standard)}".Length;

        return $"""
                {team.VoiceChannel?.Mention}
                {team.TextChannel.Mention}{team.Players.Aggregate("", (current, player) => $"{current}\n{Format(player)}")}
                """;

        string Format(Player player)
        {
            if (Match.League is League.Tournament or League.Event or League.Custom) return player.User.Mention;

            (Domain.Entities.Users.User user, QueueRole role) = player;
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            string rating = Match.League switch
            {
                League.Pro => $"{user.Rating}",
                League.Standard => $"{user.Rating_Standard}",
                _ => string.Empty
            };
            if (rating.Length < ratingLength) rating = $"{new string(' ', ratingLength - rating.Length)}{rating}";
            if (Match.League is League.Standard && user.Pro) rating = $"{new string(' ', ratingLength - 3)}Pro";

            string roleChar = Match.League switch
            {
                League.Pro or League.Standard => $" | {role.ToString()[0]}",
                _ => string.Empty
            };

            return $"`{rating}{roleChar} | {user.InGameName}`";
        }
    }
    private const string EmptyAction = "\n---------";
    private string FormatDraftSteps(List<Team.Step> steps, bool isNext, DraftAction actionType, bool scuffed = false)
    {
        string value = "";
        for (int i = 0; i < steps.Count; i++)
        {
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            string decorator = steps[i].Action switch
            {
                DraftAction.Pick => "+",
                DraftAction.Ban => "-",
                DraftAction.GlobalBan => "-",
                _ => throw new UnreachableException()
            };
            value += $"\n{decorator}{((i == steps.Count - 1 && scuffed) ? Scuffed() : $"{steps[i].Entity.Name}{(steps[i].Action is DraftAction.GlobalBan ? "(global)" : string.Empty)}")}";
        }

        int banSteps = Match.Draft.Steps.Count(s => s.Action is DraftAction.Ban or DraftAction.GlobalBan);
        int pickSteps = Match.Draft.Steps.Count(s => s.Action is DraftAction.Pick);
        string filler = actionType switch
        {
            DraftAction.Ban or DraftAction.GlobalBan =>
                string.Join(' ', Enumerable.Repeat(EmptyAction, banSteps - steps.Count)),

            DraftAction.Pick =>
                string.Join(' ', Enumerable.Repeat(EmptyAction, pickSteps - steps.Count)),

            _ => throw new UnreachableException()
        };

        if (banSteps > pickSteps && actionType is DraftAction.Pick)
            filler += string.Join(' ', Enumerable.Repeat("\n        ‎ ", banSteps - pickSteps));
        else if (pickSteps > banSteps && actionType is DraftAction.Ban)
            filler += string.Join(' ', Enumerable.Repeat("\n        ‎ ", pickSteps - banSteps));

        string turnIndicator = isNext &&
                            Match.Draft.DraftType is DraftType.Sequential &&
                            !Match.Draft.IsFinished
            ? CoreConfig.Draft.Sequential.TurnIndicator
            : "⠀";

        return $"""
                {turnIndicator}
                ```diff{value}{filler}```
                """;
    }
    private void ScuffedLastAction(Match.Side side, DiscordEmbedBuilder embed)
    {
        switch (side)
        {
            case Match.Side.Team1:

                DraftStep? lastT1Step = Match.Draft.Steps.LastOrDefault(s => s.TokenId2 != default);
                if (lastT1Step is null) return;
                if (lastT1Step.TokenId1 == default)
                {
                    // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                    switch (lastT1Step.Action)
                    {
                        case DraftAction.Pick:
                            embed.GetField(EmbedStructure.Team2Picks).Value = FormatDraftSteps(Team2.Picks, false, DraftAction.Pick, true);
                            break;

                        case DraftAction.Ban:
                        case DraftAction.GlobalBan:
                            embed.GetField(EmbedStructure.Team2Bans).Value = FormatDraftSteps(Team2.Bans, false, DraftAction.Ban, true);
                            break;

                        default: throw new UnreachableException();
                    }
                }

                break;

            case Match.Side.Team2:
                DraftStep? lastT2Step = Match.Draft.Steps.LastOrDefault(s => s.TokenId1 != default);
                if (lastT2Step is null) return;
                if (lastT2Step.TokenId2 == default)
                {
                    // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                    switch (lastT2Step.Action)
                    {
                        case DraftAction.Pick:
                            embed.GetField(EmbedStructure.Team1Picks).Value = FormatDraftSteps(Team1.Picks, false, DraftAction.Pick, true);
                            break;

                        case DraftAction.Ban:
                        case DraftAction.GlobalBan:
                            embed.GetField(EmbedStructure.Team1Bans).Value = FormatDraftSteps(Team1.Bans, false, DraftAction.Ban, true);
                            break;

                        default: throw new UnreachableException();
                    }
                }

                break;

            case Match.Side.None: break;
            default: throw new UnreachableException();
        }
    }
    private static string Scuffed()
    {
        char[] randomChars = ['▉', '▤', '▧', '▥', '▆', 'X', '?'];
        return new string(randomChars[Random.Shared.Next(randomChars.Length)], 5);
    }

    #endregion
}
