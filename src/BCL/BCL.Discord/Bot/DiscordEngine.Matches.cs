using BCL.Common.Extensions;
using BCL.Core;
using BCL.Core.Services.Abstract;
using BCL.Core.Services.Args;
using BCL.Discord.Commands.SlashCommands;
using BCL.Discord.Components.Dashboards;
using BCL.Discord.Components.Draft;
using BCL.Domain;
using BCL.Domain.Dtos;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

using Humanizer;
using Humanizer.Localisation;

using Microsoft.Extensions.DependencyInjection;

namespace BCL.Discord.Bot;
public partial class DiscordEngine
{
    private Task OnMatchStarted(object sender, MatchStartedEventArgs args)
    {
        Task.Factory.StartNew(() => StartMatch(args));
        return Task.CompletedTask;
    }
    private async Task StartMatch(MatchStartedEventArgs args)
    {
        _ = Log("Refreshing Guild cache", false, true);
        Guild = await Client.GetGuildAsync(Guild.Id, true);

        _ = Log($"**__Creating {args.Match.League} match with id `{args.Match.Id}`...__**", false, true);
        DiscordMessage message = await QueueChannel.SendMessageAsync(new DiscordMessageBuilder()
            .WithContent($"Creating **{args.Match.League}** match with id `{args.Match.Id}`..."));

        _ = Log($"Clearing auto purge jobs for`{args.Match.Id}`...", false, true);
        ClearPurgeJobs(args);

        _ = Log($"Notifying users via DM for `{args.Match.Id}`...", false, true);
        _ = DmUsers(args);

        _ = Log($"Creating inMatchRole for `{args.Match.Id}`...", false, true);
        DiscordRole inMatchRole = await CreateInMatchRole(args);

        _ = Log($"Creating team channels for `{args.Match.Id}`...", false, true);
        await CreateTeamChannel(args.Team1, args.Team2, args.Match.League is League.Tournament);
        await CreateTeamChannel(args.Team2, args.Team1, args.Match.League is League.Tournament);

        _ = Log($"Sending Player Info for `{args.Match.Id}`...", false, true);
        await SendTeamInfo(args.Team1);
        await SendTeamInfo(args.Team2);

        _ = Log($"Updating roles for `{args.Match.Id}`...", false, true);
        UpdateMatchRoles(args, inMatchRole);

        _ = Log($"Sending match embeds for `{args.Match.Id}`...", false, true);
        DiscordMatch discordMatch = new(args, inMatchRole, this
            /*,_services.CreateScope().ServiceProvider.GetService<IBankService>()!*/);

        _matchManager.ActiveMatches.Add(args.Match.Id, discordMatch);
        await discordMatch.Initialize();

        _ = Log($"OnMatchStarted finished for `{args.Match.Id}`...", false, true);
        _ = message.DeleteAsync();
    }

    #region Match Creation

    private static void ClearPurgeJobs(MatchStartedEventArgs args)
    {
        foreach (User? user in args.Team1.Users.Concat(args.Team2.Users)) QueueCommands.ClearPurgeJob(user.DiscordId);
    }
    private async Task DmUsers(MatchStartedEventArgs args)
    {
        IEnumerable<User> users = args.Team1.Users
            .Concat(args.Team2.Users)
            .Where(u => u is { IsTestUser: false, NewMatchDm: true });

        foreach (User? user in users)
        {
            try
            {
                DiscordMember? member = Guild.Members[user.DiscordId];
                if (member is null) continue;

                DiscordDmChannel dmChannel = await member.CreateDmChannelAsync();
                _ = dmChannel.SendMessageAsync($"{member.Mention} hey you have a bcl **{args.Match.League}** game!");
            }
            catch (Exception e)
            {
                _ = Log(e, null, false, true);
            }
        }
    }
    private async Task SendTeamInfo(Team team)
    {
        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithAuthor($"{team.Name}", null, Guild.IconUrl)
            .WithColor(new DiscordColor(team.Captain.ProfileColor))
            .WithDescription($"Original Captain: {team.Captain.InGameName} {team.Captain.Mention}")
            .WithTimestamp(DateTime.UtcNow)
            .WithFooter(CoreConfig.Version, Guild.IconUrl);

        int index = 0;
        foreach ((User user, QueueRole role) in team.Players)
        {
            index++;
            int meleeLines = user.DefaultMelee.Split("\n").Length;
            int rangedLines = user.DefaultRanged.Split("\n").Length;
            int supportLines = user.DefaultSupport.Split("\n").Length;
            int maxLines = Math.Max(meleeLines, Math.Max(rangedLines, supportLines));
            string empty = $"\n{new string('⠀', 10)}"; //WhiteSpace but not really '⠀'

            embed.AddField(user.InGameName, $"""

                                             {user.Mention} Average draft time: **{(user.CurrentSeasonStats.TimedDrafts > 0 ? user.CurrentSeasonStats.AverageDraftTime.Humanize(precision: 3, minUnit: TimeUnit.Second) : "N/A")}**
                                             Prefers: **{role}** | Captain Winrate: **{(user.CurrentSeasonStats.LatestSnapshot?.CaptainGames > 0 ? $"{(double)user.CurrentSeasonStats.LatestSnapshot.CaptainWins / user.CurrentSeasonStats.LatestSnapshot.CaptainGames:0.00%}" : "N/A")}**
                                             Last Played: {user.LastPlayed?.DiscordTime(DiscordTimeFlag.R) ?? "Never :zzz:"} | Games Played in `{DomainConfig.Season}`: **{user.CurrentSeasonStats.LatestSnapshot?.GamesPlayed ?? 0}**
                                             """);
            embed.AddField("Melee", $"""
                                     ```{user.Styling}
                                     {user.DefaultMelee}{string.Concat(Enumerable.Repeat(empty, maxLines - meleeLines))}```
                                     """, true);
            embed.AddField("Ranged", $"""
                                      ```{user.Styling}
                                      {user.DefaultRanged}{string.Concat(Enumerable.Repeat(empty, maxLines - rangedLines))}```
                                      """, true);
            embed.AddField("Support", $"""
                                       ```{user.Styling}
                                       {user.DefaultSupport}{string.Concat(Enumerable.Repeat(empty, maxLines - supportLines))}```
                                       """, true);

            if (index != team.Players.Count) embed.AddField("⠀", "⠀"); //kekL
        }

        await team.TextChannel.SendMessageAsync(embed);
    }
    private async Task CreateTeamChannel(Team team, Team enemyTeam, bool voice = false)
    {
        DiscordChannel textChannel = await Guild.CreateTextChannelAsync(team.Name, Guild.GetChannel(DiscordConfig.Channels.QueueCategoryId));
        DiscordChannel? voiceChannel = null;
        if (voice) voiceChannel = await Guild.CreateVoiceChannelAsync(team.Name, Guild.GetChannel(DiscordConfig.Channels.QueueCategoryId));

        team.TextChannel = textChannel;
        team.VoiceChannel = voiceChannel;

        await textChannel.AddOverwriteAsync(Guild.EveryoneRole, deny: Permissions.AccessChannels);

        //TODO optimize idk

        foreach (DiscordMember member in team.Users
            .Where(u => !u.IsTestUser)
            .Select(user => Guild.Members[user.DiscordId]))
        {
            await textChannel.AddOverwriteAsync(member, Permissions.AccessChannels);
            if (voice && voiceChannel is not null) await voiceChannel.AddOverwriteAsync(member, Permissions.AccessChannels);
        }

        foreach (DiscordMember member in enemyTeam.Users
             .Where(u => !u.IsTestUser)
             .Select(user => Guild.Members[user.DiscordId]))
        {
            await textChannel.AddOverwriteAsync(member, deny: Permissions.AccessChannels);
            if (voice && voiceChannel is not null) await voiceChannel.AddOverwriteAsync(member, deny: Permissions.AccessChannels);
        }
    }
    private async Task<DiscordRole> CreateInMatchRole(MatchStartedEventArgs args)
    {
        DiscordRole? inMatchRole = null;
        //Dev server doesn't have discord nitro boost level 3 xddd
        if (!DiscordConfig.IsTestBot)
        {
            try
            {
                using HttpResponseMessage httpResponse = await _httpClient.GetAsync(new Uri(Guild.IconUrl));
                httpResponse.EnsureSuccessStatusCode();
                //await using var iconStream = await httpResponse.Content.ReadAsStreamAsync();
                //iconStream.Seek(0, SeekOrigin.Begin);

                inMatchRole = await Guild.CreateRoleAsync(
                    $"InMatch_{args.Match.Id}",
                    color: DiscordColor.Orange,
                    hoist: true,
                    mentionable: true,
                    //icon: iconStream);
                    icon: null);
            }
            catch (UnauthorizedException ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine(ex.JsonMessage);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        //if on dev or the icon thing failed w/e
        if (inMatchRole == null)
        {
            inMatchRole = await Guild.CreateRoleAsync(
                $"InMatch_{args.Match.Id}",
                color: DiscordColor.Orange,
                hoist: true,
                mentionable: true);
        }

        List<DiscordRole> roles = Guild.Roles.OrderBy(r => r.Value.Position).Select(r => r.Value).ToList();
        int position = roles.IndexOf(Guild.Roles[DiscordConfig.Roles.InQueueId]);
        roles.Insert(position, inMatchRole);
        Dictionary<int, DiscordRole> reorderedRoles = [];
        int index = 1;
        roles.ForEach(r => reorderedRoles.Add(index++, r));
        await Guild.ModifyRolePositionsAsync(reorderedRoles);

        return inMatchRole;
    }
    private void UpdateMatchRoles(MatchStartedEventArgs args, DiscordRole inMatchRole)
    {
        UpdateTeamRoles(args.Team1.Users, inMatchRole);
        UpdateTeamRoles(args.Team2.Users, inMatchRole);
    }
    private void UpdateTeamRoles(IEnumerable<User> users, DiscordRole inMatchRole)
    {
        DiscordRole inQueueRole = Guild.Roles[DiscordConfig.Roles.InQueueId];

        foreach (User? user in users.Where(u => !u.IsTestUser))
        {
            try
            {
                DiscordMember discordMember = Guild.Members[user.DiscordId];

                List<DiscordRole> roles = discordMember.Roles.ToList();
                roles.Remove(inQueueRole);
                roles.Add(inMatchRole);
                discordMember.ReplaceRolesAsync(roles.DistinctBy(r => r.Id));
            }
            catch (Exception e)
            {
                _ = Log($"Updating roles for {user.Name} threw an exception: {e}");
            }
        }
    }

    #endregion

    #region Match Finalization

    public void FinishMatch(DiscordRole discordRole, MatchOutcome matchOutcome)
    {
        DiscordMatch? discordMatch = _matchManager.GetMatch(discordRole); if (discordMatch is null) return;
        FinishMatch(discordMatch, matchOutcome);
    }
    private void FinishMatch(DiscordMatch discordMatch, MatchOutcome matchOutcome)
    {
        discordMatch.Match.Outcome = matchOutcome;
        Task.Run(() => FinishMatch(discordMatch, matchOutcome is MatchOutcome.Canceled ? "Dropped by admin." : null));
    }

    private static readonly SemaphoreSlim FinishMatchGate = new(1, 1);
    public async Task FinishMatch(DiscordMatch discordMatch, string? cancelReason = null)
    {
        await FinishMatchGate.WaitAsync(10000);

        discordMatch.Match.CancelReason = cancelReason;

        try
        {
            using IServiceScope scope = _services.CreateScope();
            //await scope.ServiceProvider.GetRequiredService<IBankService>().PredictionPayout(discordMatch.Match.Id);
            await scope.ServiceProvider.GetRequiredService<IMatchService>().FinishMatch(discordMatch.Match.Id);

            foreach (DiscordMember? member in (discordMatch.Team1.VoiceChannel?.Users ?? [])
                         .Concat(discordMatch.Team2.VoiceChannel?.Users ?? []))
            {
                try
                {
                    _ = member.PlaceInAsync(Guild.Channels[DiscordConfig.Channels.GeneralVoiceId]);
                }
                catch (NotFoundException) { /*ignored*/ }
                catch (Exception exception) { _ = Log(exception); }
            }

            try
            {
                _ = discordMatch.MatchRole.DeleteAsync("Match finished");
                _ = discordMatch.Team1.TextChannel.DeleteAsync("Match finished");
                _ = discordMatch.Team2.TextChannel.DeleteAsync("Match finished");
                _ = discordMatch.Team1.VoiceChannel?.DeleteAsync("Match finished");
                _ = discordMatch.Team2.VoiceChannel?.DeleteAsync("Match finished");
            }
            catch (NotFoundException) { /*ignored*/ }
            catch (Exception exception) { _ = Log(exception); }

            await ArchiveMatch(discordMatch);
        }
        catch (NotFoundException) { /*ignored*/ }
        catch (Exception exception) { _ = Log(exception); }

        FinishMatchGate.Release();
    }
    private async Task ArchiveMatch(DiscordMatch discordMatch)
    {
        _ = discordMatch.QueueChannelMessage.DeleteAsync("Match concluded");

        DiscordMessageBuilder message = new DiscordMessageBuilder().WithEmbed(discordMatch.ArchiveEmbed());
        if (discordMatch.Match.Outcome is MatchOutcome.Canceled) message.WithContent($"{Guild.Roles[DiscordConfig.Roles.StaffId].Mention} Please investigate why the match was dropped :)");
        _matchManager.ActiveMatches.Remove(discordMatch.Match.Id);

        DiscordMessage matchHistoryMessage = discordMatch.Match.Outcome is MatchOutcome.Canceled
            ? await DroppedMatchesChannel.SendMessageAsync(message)
            : await MatchHistoryChannel.SendMessageAsync(message);

        _services.CreateScope().ServiceProvider.GetService<IMatchService>()!.UpdateDraftTimeAndLink(discordMatch.Match.Id, discordMatch.DraftTimes, matchHistoryMessage);
    }

    #endregion
}
