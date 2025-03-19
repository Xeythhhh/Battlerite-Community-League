using BCL.Discord.Extensions;
using BCL.Domain;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;

using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace BCL.Discord.Commands.SlashCommands.Users;
public partial class UserCommands
{
    private async Task _MatchHistory(BaseContext context, DiscordUser discordUser, User user, string season)
    {
        season = season == "All" ? DomainConfig.Season : season;

        await context.FollowUpAsync(new DiscordFollowupMessageBuilder()
            .WithContent($"Generating match history for season `{season}` for {discordUser.Mention}..."));
        Domain.Entities.Users.Stats? stats = user.SeasonStats.FirstOrDefault(ss => ss.Season == season);
        List<Domain.Entities.Matches.Match> matches = stats is null
            ? []
            : matchRepository.Get(m =>
                    m.Season == season &&
                    m._discordUserIds.Contains(discordUser.Id.ToString()))
                .ToList();

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithAuthor($"{user.InGameName} - {season}", null, discordUser.AvatarUrl)
            .WithTitle("Match History")
            .WithDescription($"{discordUser.Mention} {(user.RoleId is 0 ? string.Empty : user.RoleMention)}")
            .WithColor(new DiscordColor(user.ProfileColor))
            .WithTimestamp(user.LastPlayed)
            .WithFooter("Last played", discordEngine.Guild.IconUrl);

        if (stats == null)
        {
            embed.AddField("Error", $"""

                                     ```diff
                                     -User has not played in `{season}`
                                     ```
                                     """);
            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(embed));
            return;
        }

        if (stats.PlayedStandard)
            embed.AddMatchHistory(discordUser, user, matches, mapRepository, League.Standard);
        if (stats.PlayedPro)
            embed.AddMatchHistory(discordUser, user, matches, mapRepository, League.Pro);

        if (user.MatchHistory_DisplayEvent && stats.PlayedEvent)
            embed.AddMatchHistory(discordUser, user, matches, mapRepository, League.Event);
        if (user.MatchHistory_DisplayTournament && stats.PlayedTournament)
            embed.AddMatchHistory(discordUser, user, matches, mapRepository, League.Tournament);
        if (user.MatchHistory_DisplayCustom && stats.PlayedCustom)
            embed.AddMatchHistory(discordUser, user, matches, mapRepository, League.Custom);

        await context.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(embed));
    }
}
