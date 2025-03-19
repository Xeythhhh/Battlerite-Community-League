using BCL.Discord.Components.Draft;

using DSharpPlus.Entities;

namespace BCL.Discord.Components;
public class MatchManager
{
    public Dictionary<Ulid, DiscordMatch> ActiveMatches { get; set; } = [];

    public IEnumerable<DiscordChannel> Channels => ActiveMatches.Values.SelectMany(m => m.Channels);
    public IEnumerable<DiscordRole> Roles => ActiveMatches.Values.Select(m => m.MatchRole);
    public IEnumerable<DiscordMessage> Messages => ActiveMatches.Values.SelectMany(m => m.Messages);

    public DiscordMatch? GetMatch(DiscordUser discordUser)
        => ActiveMatches.Values.FirstOrDefault(dm => dm.Match.DiscordUserIds.Contains(discordUser.Id));

    public DiscordMatch? GetMatch(DiscordRole discordRole)
        => ActiveMatches.Values.FirstOrDefault(dm => dm.MatchRole.Id == discordRole.Id);

    public DiscordMatch? GetMatchFromMessage(DiscordMessage discordMessage)
        => ActiveMatches.Values.FirstOrDefault(dm => dm.Messages.Any(msg => msg.Id == discordMessage.Id));

    public DiscordMatch? GetMatch(Ulid id) => ActiveMatches.Values.FirstOrDefault(dm => dm.Match.Id == id);
    public DiscordMatch? GetMatch(string matchId) => Ulid.TryParse(matchId, out Ulid id) ? GetMatch(id) : null;
}
