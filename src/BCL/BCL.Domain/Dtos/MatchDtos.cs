using BCL.Domain.Enums;

namespace BCL.Domain.Dtos;
public record DraftTime(ulong DiscordId, TimeSpan Duration);
public record MatchResult(MatchOutcome Outcome, string? CancelReason = null);
