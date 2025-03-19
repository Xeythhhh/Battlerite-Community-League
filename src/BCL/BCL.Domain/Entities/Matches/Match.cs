using BCL.Domain.Entities.Abstract;
using BCL.Domain.Enums;

#pragma warning disable CS8618
namespace BCL.Domain.Entities.Matches;

public partial class Match : Entity
{
    public string? BotVersion { get; set; } = null;
    public Draft Draft { get; set; }
    public Ulid MapId { get; set; }
    public Region Region { get; set; }
    public MatchOutcome Outcome { get; set; } = MatchOutcome.InProgress;
    public League League { get; set; }
    public string Team1 { get; set; } // user1_Id|user1_discordId|user1_role user2_Id|user2_discordId|user2_role user3_Id|user3_discordId|user3_role ...
    public string Team2 { get; set; }
    public string? CancelReason { get; set; }
    public string? Season { get; set; }
    public double EloShift { get; set; }
    public MatchmakingLogic MatchmakingLogic { get; set; }
    public Uri JumpLink { get; set; } = new Uri("about:blank");

    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
    public bool HasJumpLink => JumpLink is null
                               || JumpLink.OriginalString != "about:blank"
                               || string.IsNullOrWhiteSpace(JumpLink.OriginalString);
}
