using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

using BCL.Domain.Entities.Queue;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;

using DSharpPlus.Entities;

namespace BCL.Domain.Entities.Matches;
public partial class Match
{
    [NotMapped] public ulong Team1ChannelId { get; set; } = 0;
    [NotMapped] public ulong Team2ChannelId { get; set; } = 0;

    // ReSharper disable once InconsistentNaming
    public string _discordUserIds { get; set; } = string.Empty;

    [NotMapped]
    public IEnumerable<ulong> DiscordUserIds => _discordUserIds
        .Split(" ")
        .Select(s => ulong.Parse(s));

    [NotMapped]
    public IEnumerable<Match_PlayerInfo> PlayerInfos => Team1_PlayerInfos.Concat(Team2_PlayerInfos);

    // ReSharper disable InconsistentNaming
    [NotMapped] public IEnumerable<Match_PlayerInfo> Team1_PlayerInfos => Team1.Split(" ").Select(GetPlayerInfo);
    [NotMapped] public IEnumerable<Match_PlayerInfo> Team2_PlayerInfos => Team2.Split(" ").Select(GetPlayerInfo);
    public record Match_PlayerInfo(Ulid Id, ulong DiscordId, QueueRole Role);
    private static Match_PlayerInfo GetPlayerInfo(string p)
    {
        string[] x = p.Split("|");
        Ulid id = Ulid.Empty;
        ulong discordId = default;
        QueueRole role = QueueRole.Fill;
        try
        {
            // Don't change this shit, it looks funky but it's for test users
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1806 // Do not ignore method results
            Ulid.TryParse(x[0], out id);
            ulong.TryParse(x[1], out discordId);
            Enum.TryParse<QueueRole>(x[2], true, out role);
#pragma warning restore CA1806 // Do not ignore method results
#pragma warning restore IDE0079 // Remove unnecessary suppression
        }
        catch
        {
            // ignored
        }

        return new Match_PlayerInfo(id, discordId, role);
    }

    // ReSharper restore InconsistentNaming

    public Side GetSide(DiscordUser discordUser) => GetSide(discordUser.Id);
    public Side GetSide(ulong discordUserId) => GetSide(discordUserId.ToString());
    public Side GetSide(User user) => GetSide(user.Id.ToString());
    public Side GetSide(string id) => id switch
    {
        _ when Team1.Contains(id) => Side.Team1,
        _ when Team2.Contains(id) => Side.Team2,
        _ => Side.None
    };

    public enum Side
    {
        Team1,
        Team2,
        None,
        Both
    }

    public bool IsCaptain(User user) => IsCaptain(user.DiscordId, out _);
    public bool IsCaptain(DiscordUser discordUser) => IsCaptain(discordUser.Id, out _);
    public bool IsCaptain(DiscordUser discordUser, out bool isTeam1) => IsCaptain(discordUser.Id, out isTeam1);
    public bool IsCaptain(ulong discordUserId, out bool isTeam1)
    {
        bool isCaptain = Draft.Captain1DiscordId == discordUserId || Draft.Captain2DiscordId == discordUserId;
        isTeam1 = GetSide(discordUserId) switch
        {
            Side.Team1 => true,
            _ => false
        };

        return isCaptain;
    }

    // ReSharper disable once InconsistentNaming
    public enum _Team { Team1, Team2 }
    public bool Picked(_Team team, Champion champion) => Draft.Steps.Any(s => (s.Action is DraftAction.Pick) && team switch
    {
        _Team.Team1 => s.TokenId1 == champion.Id,
        _Team.Team2 => s.TokenId2 == champion.Id,
        _ => throw new UnreachableException()
    });
    public bool Banned(_Team team, Champion champion) => Draft.Steps.Any(s => (s.Action is DraftAction.Ban or DraftAction.GlobalBan) && team switch
    {
        _Team.Team1 => s.TokenId1 == champion.Id,
        _Team.Team2 => s.TokenId2 == champion.Id,
        _ => throw new UnreachableException()
    });
    public bool Won(Champion champion) => Draft.Steps.Any(s =>
        s.Action is DraftAction.Pick &&
        ((s.TokenId1 == champion.Id && Outcome == MatchOutcome.Team1) ||
         (s.TokenId2 == champion.Id && Outcome == MatchOutcome.Team1)));

    public bool Won(User user) =>
        GetSide(user) switch
        {
            Side.Team1 when Outcome is MatchOutcome.Team1 => true,
            Side.Team2 when Outcome is MatchOutcome.Team2 => true,
            _ => false
        };

    public class EloModifier(
        string label,
        double factor,
        bool applyToLossesOnly,
Side appliesToTeam,
        Func<List<User>, User, bool>? predicate = null)
    {
        public string Label { get; set; } = label;
        public double Factor { get; set; } = factor;
        public bool ApplyToLossesOnly { get; set; } = applyToLossesOnly;
        public Side AppliesTo { get; set; } = appliesToTeam;
        public Func<List<User>, User, bool> Predicate { get; set; } = predicate ?? ((_, _) => true);

        public static EloModifier DoubleUp { get; set; } = new("DoubleUp!", 2, false, Side.Both);

        public static readonly string PlacementsLabel = "Player(s) in placements!";
        public static EloModifier TeammateInPlacements(Side modifierAppliesTo)
            => new(
                PlacementsLabel,
                0.5,
                true,
                modifierAppliesTo,
                (team, user) => team.Any(p => !user.IsInPlacements_Standard && p.IsInPlacements_Standard
                        && p.Id != user.Id));

        public static readonly string HighSkillGapLabel = "High skill gap!";
        public static EloModifier HighSkillGap(double factor, Side modifierAppliesTo)
            => new(
                HighSkillGapLabel,
                factor,
                true,
                modifierAppliesTo,
                (team, user) => team.Where(p => p.Id != user.Id)
                    .All(p => user.Rating_Standard >= p.Rating_Standard));
    }

    [NotMapped] public List<EloModifier> EloModifiers { get; set; } = [];
    [NotMapped] public List<(Ulid, double)> PlayerEloShifts { get; set; } = [];
}
