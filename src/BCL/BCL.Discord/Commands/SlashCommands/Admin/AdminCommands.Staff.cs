using BCL.Common.Extensions;
using BCL.Core;
using BCL.Core.Services;
using BCL.Core.Services.Abstract;
using BCL.Core.Services.Queue;
using BCL.Discord.Attributes.Permissions;
using BCL.Discord.Bot;
using BCL.Discord.Components;
using BCL.Discord.Extensions;
using BCL.Discord.OptionProviders;
using BCL.Domain;
using BCL.Domain.Dtos;
using BCL.Domain.Entities.Matches;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;

#pragma warning disable CS4014

namespace BCL.Discord.Commands.SlashCommands.Admin;
public partial class AdminCommands : ApplicationCommandModule
{
    [SlashCommand_Staff]
    [SlashCommandGroup("Staff", "Staff commands", false)]
    [SlashModuleLifespan(SlashModuleLifespan.Scoped)]
    public class Staff(
        IUserRepository userRepository,
        IMatchRepository matchRepository,
        IMapRepository mapRepository,
        IMatchmakingService matchmakingService,
        IMatchService matchService,
        ITeamRepository teamRepository,
        MatchManager matchManager,
        DiscordEngine discordEngine) : ApplicationCommandModule
    {
        [SlashCommand_Admin]
        [SlashCommand("RevertMatch", "Revert any MMR changes made by a match.")]
        public async Task RevertMatch(InteractionContext context,
            [Option("MatchId", "Id of the match you want to revert")] string matchId)
        {
            await context.CreateResponseAsync($"Reverting changes for match with id `{matchId}`.");
            if (!Ulid.TryParse(matchId, out Ulid id)) return;
            Match? match = await matchRepository.GetByIdAsync(id);
            if (match == null || match.Outcome is MatchOutcome.Canceled)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"Match with id `{matchId}` not found or canceled."));
                return;
            }
            if (match.Season != DomainConfig.Season) return;

            match.Outcome = MatchOutcome.Canceled;
            IEnumerable<User?> team1 = match.Team1_PlayerInfos.Select(p => userRepository.GetById(p.Id));
            IEnumerable<User?> team2 = match.Team2_PlayerInfos.Select(p => userRepository.GetById(p.Id));

            List<User?> team1Players = team1.ToList();
            foreach (User? player in team1Players.Where(player => player != null))
            {
                double eloShift = player!.GetStats(match.Season)?
                    .Snapshots.Single(s => s.MatchId == match.Id)
                    .Eloshift
                               ?? match.EloShift;

                switch (match.League)
                {
                    case League.Pro:
                        player.Rating -= eloShift;
                        player.CurrentSeasonStats.LatestSnapshot!.Rating -= eloShift;
                        break;

                    case League.Standard:
                        player.Rating_Standard -= eloShift;
                        player.CurrentSeasonStats.LatestSnapshot!.Rating_Standard -= eloShift;
                        break;

                    case League.Event:
                    case League.Tournament:
                    case League.Custom:
                    case League.Premade3V3:
                        throw new NotImplementedException();

                    default:
                        throw new ArgumentOutOfRangeException(nameof(matchId));
                }
            }

            List<User?> team2Players = team2.ToList();
            foreach (User? player in team2Players.Where(player => player != null))
            {
                double eloShift = player!.GetStats(match.Season)?
                                   .Snapshots.Single(s => s.MatchId == match.Id)
                                   .Eloshift
                               ?? -match.EloShift;
                switch (match.League)
                {
                    case League.Pro:
                        player.Rating -= eloShift;
                        player.CurrentSeasonStats.LatestSnapshot!.Rating -= match.EloShift;
                        break;

                    case League.Standard:
                        player.Rating_Standard -= match.EloShift;
                        player.CurrentSeasonStats.LatestSnapshot!.Rating_Standard -= match.EloShift;
                        break;

                    case League.Event:
                    case League.Tournament:
                    case League.Custom:
                    case League.Premade3V3:
                        throw new NotImplementedException();

                    default:
                        throw new ArgumentOutOfRangeException(nameof(matchId));
                }
            }

            await userRepository.SaveChangesAsync();

            string content = $"""
                           {context.User.Mention}
                           Reverted MMR changes made by match with id `{matchId}`. New Ratings:
                           {team1Players.Concat(team2Players).Aggregate("", (current, player) => $"{current}\n> {player!.Mention} : `S {player.Rating_Standard}`|`P {player.Rating}`")}
                           """;

            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent(content));
            await discordEngine.Log(content);
        }

        [SlashCommand("PurgeBrokenMatch", "Yes", false)]
        public async Task PurgeBrokenMatch(InteractionContext context,
            [Option("MatchId", "id of the match")] string matchId)
        {
            if (Ulid.TryParse(matchId, out Ulid id))
            {
                Components.Draft.DiscordMatch? discordMatch = matchManager.GetMatch(id);

                if (discordMatch is null)
                {
                    Match? match = MatchService.ActiveMatches.SingleOrDefault(m => m.Id == id);
                    if (match is null) return;

                    await matchService.FinishMatch(id);
                    DiscordMessage feedback = await context.Channel.SendMessageAsync($"Fixing broken match `{match.Id}`...");
                    await discordEngine.RefreshQueueTracker(feedback, true);
                    await feedback.DeleteAsync(10);

                    return;
                }

                await context.Channel.SendMessageAsync($"Use {context.Client.MentionCommand<Staff>(nameof(FinishMatch))}");
                return;
            }

            await context.Channel.SendMessageAsync("Invalid Id");
        }

        [SlashCommand("FinishMatch", "Manually finish a match")]
        public async Task FinishMatch(InteractionContext context,
            [Option("match", "The match role representing the match you want to finish")] DiscordRole matchRole,
            [Option("outcome", "Match outcome")] MatchOutcome outcome)
        {
            await context.CreateResponseAsync($"Finishing match {matchRole.Mention} with outcome {outcome}...");

            if (outcome is MatchOutcome.InProgress)
            {
                await context.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("Please stop being dumb")
                    .AddMention(new UserMention(context.User)));
                return;
            }

            Components.Draft.DiscordMatch? discordMatch = matchManager.GetMatch(matchRole);
            if (discordMatch is null)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                        $"{context.User.Mention} No match found for __role__ {matchRole.Mention}.⚠️ (Removing related entities)")
                    .AddMention(new UserMention(context.User)));

                //assume it broke
                string? id = matchRole.Name.Split("_").LastOrDefault();
                if (id is not null) await PurgeBrokenMatch(context, id);

                return;
            }

            discordEngine.FinishMatch(matchRole, outcome);
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Match with id `{discordMatch.Match.Id}` finished by {context.User.Mention}."));
        }

        [SlashCommand("EnableQueue", "Enables the queue")]
        public async Task EnableQueue(InteractionContext context,
            [Option("TestMode", "Matches played in testMode do not get saved into the database.")] bool testMode = false)
        {
            await context.DeferAsync();

            QueueService.TestMode = testMode;
            QueueService.Enabled = true;
            string content = "Queue has been **enabled**, have fun gamers!";
            if (testMode) content += "\n Currently running in __test mode__, matches do not count.⚠️";

            DiscordMessage feedback = await context.Channel.SendMessageAsync("Enabling Queue...");
            await discordEngine.RefreshQueueTracker(feedback, true);
            await feedback.DeleteAsync(10);

            await context.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(content));
        }

        [SlashCommand("DisableQueue", "Disables the queue")]
        public async Task DisableQueue(InteractionContext context,
            [Option("reason", "Reason for disabling queue")] string reason)
        {
            await context.DeferAsync();

            QueueService.Enabled = false;
            QueueService.DisabledReason = reason;
            string content = $"Queue has been **disabled** by {context.User.Mention}.⚠️\nReason: {reason}";

            DiscordMessage feedback = await context.Channel.SendMessageAsync($"""
                                                                   Disabling Queue...
                                                                   Reason: {reason} ⚠️
                                                                   """);
            await discordEngine.RefreshQueueTracker(feedback, true);
            await feedback.DeleteAsync(10);

            discordEngine.Log(content);
            await context.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(content));
        }

        [SlashCommand("Matchmaking", "Sets the matchmaking logic")]
        public async Task Matchmaking(InteractionContext context,
            [Option("queue", "Queue for which you want to change matchmaking logic")] League queue,
            [Option("logic", "Pick the logic to be used in matchmaking")] MatchmakingLogic logic,
            [Option("draftType", "Pick the draft type to be used in matchmaking")] DraftType draftType,
            [Option("draftFormat", "Chanel the draft format")] string? draftFormat = null)
        {
            string content = $"Matchmaking logic set to __{logic}__ with __{draftType}__ draft for **{queue} League** by {context.User.Mention}";
            switch (queue)
            {
                case League.Pro:
                    MatchmakingService.ProLogic = logic;
                    MatchmakingService.ProDraftType = draftType;
                    break;

                case League.Standard:
                    MatchmakingService.StandardLogic = logic;
                    MatchmakingService.StandardDraftType = draftType;
                    break;

                case League.Event:
                case League.Tournament:
                case League.Custom:
                case League.Premade3V3:
                    throw new NotImplementedException();

                default:
                    throw new ArgumentOutOfRangeException(nameof(queue), queue, null);
            }

            if (draftFormat is not null)
            {
                string[] validSteps = ["GB", /*"MB", "MP", "R", */"B", "P"];
                string[] steps = draftFormat.Split("-");
                bool validDraftFormat = steps.All(s => validSteps.Any(vs => vs.Equals(s, StringComparison.CurrentCultureIgnoreCase)));

                if (!validDraftFormat)
                {
                    await context.CreateResponseAsync($"Invalid __DraftFormat__ `{draftFormat}`.⚠️\nValid steps: `{string.Join(" | ", validSteps)}`");
                    return;
                }

                switch (queue)
                {
                    case League.Pro:
                        CoreConfig.Draft.ProFormatRaw = draftFormat;
                        break;

                    case League.Standard:
                        CoreConfig.Draft.FormatRaw = draftFormat;
                        break;
                }

                content = content.Replace("draft", $"draft with format `{draftFormat}`");
            }

            await context.CreateResponseAsync(content);
            discordEngine.Log(content);
        }

        [SlashCommand("CreatePremadeMatch", "Creates a custom premade teams match")]
        public async Task CreatePremadeMatch(InteractionContext context,
            [Option("League", "Type of match you want to create")] League league,
            [Option("DraftType", "Type of draft you want to use")] DraftType draftType,
            [Option("Region", "Region")] Region region,
            [ChoiceProvider(typeof(MapChoiceProvider))]
            [Option("Map", "Game map")] string? mapId = null,
            [Option("DraftFormat", "Draft format override")] string? draftFormat = null
            )
        {
            if (league is League.Pro or League.Standard or League.Premade3V3) throw new Exception("Invalid League");

            await context.DeferAsync();

            //TODO DOES SOME WEIRD BAD REQUEST SHIT IDK PUT A BREAKPOINT AND SEE
            draftFormat ??= CoreConfig.Draft.ProFormatRaw;
            List<string> format = [.. draftFormat.Split('-')];
            Domain.Entities.Queue.Map? map = mapId is not null ? await mapRepository.GetByIdAsync(mapId) : null;

            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"{context.User.Mention} Please @ one member from each team!"));

            DSharpPlus.Interactivity.InteractivityExtension interactivity = context.Client.GetInteractivity();
            DSharpPlus.Interactivity.InteractivityResult<DiscordMessage> response = await interactivity.WaitForMessageAsync(m => m.Author.Id == context.User.Id);
            if (response.TimedOut)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Match creation __timed out__.⚠️"));
                return;
            }

            if (response.Result.MentionedUsers.Count != 2)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Match creation canceled, mention one member from each team when prompted.⚠️"));
                return;
            }

            DiscordUser team1Member = response.Result.MentionedUsers[0];
            DiscordUser team2Member = response.Result.MentionedUsers[1];
            response.Result.DeleteAsync();

            User? team1User = userRepository.GetByDiscordUser(team1Member);
            User? team2User = userRepository.GetByDiscordUser(team2Member);
            if (team1User is null || team1User.TeamId == Ulid.Empty ||
                team2User is null || team2User.TeamId == Ulid.Empty ||
                team1User.TeamId == team2User.TeamId)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Invalid teams."));
                return;
            }

            PremadeTeam? team1 = await teamRepository.GetByIdAsync(team1User.TeamId);
            PremadeTeam? team2 = await teamRepository.GetByIdAsync(team2User.TeamId);

            if (team1 is null ||
                team2 is null ||
                team1.Size != team2.Size ||
                !team1.IsValid ||
                !team2.IsValid)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Invalid teams."));
                return;
            }

            foreach (User? member in team1.Members.Where(member => member.TeamId != team1.Id))
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"{member.Mention} does not have __{team1.Name}__ as their active team."));
                return;
            }

            foreach (User? member in team2.Members.Where(member => member.TeamId != team2.Id))
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"{member.Mention} does not have __{team2.Name}__ as their active team."));
                return;
            }

            Match match = await matchmakingService.CreateMatch(
                team1.Members.Concat(team2.Members)
                    .Select(u => new Player(u)).ToList(),
                region,
                League.Premade3V3,
                format,
                MatchmakingLogic.None,
                map,
                team1.Name,
                team2.Name,
                draftType);

            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"**{league}** match created with __{draftType}__ draft, `{draftFormat}` format and id `{match.Id}`"));
        }

        [SlashCommand("CreateMatch", "Creates a custom match")]
        public async Task CreateMatch(InteractionContext context,
            [Option("MatchType", "Type of match you want to create")] League league,
            [Option("DraftType", "Type of draft you want to use")] DraftType draftType,
            [Option("Region", "Region")] Region region,
            [ChoiceProvider(typeof(MapChoiceProvider))]
            [Option("Map", "Game map")] string? mapId = null,
            [Option("DraftFormat", "Draft format override")] string? draftFormat = null)
        {
            if (league is League.Pro or League.Standard or League.Premade3V3) throw new Exception("Invalid League");

            draftFormat ??= CoreConfig.Draft.ProFormatRaw;
            List<string> format = [.. draftFormat.Split('-')];
            Domain.Entities.Queue.Map? map = mapId is not null ? await mapRepository.GetByIdAsync(mapId) : null;
            List<Player> players = [];

            await context.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .WithContent($"{context.User.Mention} Please input **Team 1** __name__ followed by the __player mentions__: ")
                .AddMention(new UserMention(context.User)));

            DSharpPlus.Interactivity.InteractivityExtension interactivity = context.Client.GetInteractivity();
            DSharpPlus.Interactivity.InteractivityResult<DiscordMessage> response = await interactivity.WaitForMessageAsync(m => m.Author.Id == context.User.Id);
            if (response.TimedOut) throw new Exception("Match creation __timed out__.⚠️");

            string info = $"\n**Team 1**: {response.Result.Content}";
            string team1Name = response.Result.Content.Split("<").First().Trim();
            IReadOnlyList<DiscordUser> team1 = response.Result.MentionedUsers;
            response.Result.DeleteAsync();

            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"{context.User.Mention} Please input **Team 2** __name__ followed by the __player mentions__: {info}")
                .AddMention(new UserMention(context.User)));

            response = await interactivity.WaitForMessageAsync(m => m.Author.Id == context.User.Id);
            if (response.TimedOut) throw new Exception("Match creation __timed out__.⚠️");

            info += $"\n**Team 2**: {response.Result.Content}";
            string team2Name = response.Result.Content.Split("<").First().Trim();
            IReadOnlyList<DiscordUser> team2 = response.Result.MentionedUsers;
            response.Result.DeleteAsync();

            foreach (DiscordUser? mentionedUser in team1.Concat(team2))
            {
                User? user = userRepository.GetByDiscordId(mentionedUser.Id);
                if (user is null)
                {
                    await context.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent($"{mentionedUser.Mention} is __not registered__ for bcl. Match creation __canceled__.⚠️"));
                    return;
                }

                bool isInMatch = MatchService.IsInMatch(user);
                if (isInMatch)
                {
                    await context.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent($"{mentionedUser.Mention} is in an __active match__. Match creation __canceled__.⚠️"));
                    return;
                }

                QueueService._Leave(user.DiscordId);
                await context.Guild.Members[user.DiscordId].RevokeRoleAsync(context.Guild.Roles[DiscordConfig.Roles.InQueueId]);
                players.Add(new Player(user));
            }

            if (players.Count is 0 ||
                players.Count % 2 != 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Invalid __teams__. Match creation __canceled__.⚠️"));
                return;
            }

            Match match = await matchmakingService.CreateMatch(
                players,
                region,
                league,
                format,
                MatchmakingLogic.None,
                map,
                team1Name,
                team2Name,
                draftType);

            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"**{league}** match created with __{draftType}__ draft, `{draftFormat}` format and id `{match.Id}`{info}"));
        }

        [SlashCommand_Admin]
        [SlashCommand("ProDrop", "Use this if a pro game was dropped")]
        public async Task ProDrop(InteractionContext context,
            [Option("Offender", "Discord user")] DiscordUser offenderDiscordUser,
            [Option("EnemyPlayer1", "Discord user")] DiscordUser discordUser1,
            [Option("EnemyPlayer2", "Discord user")] DiscordUser discordUser2,
            [Option("EnemyPlayer3", "Discord user")] DiscordUser discordUser3)
        {
            User? offender = userRepository.GetByDiscordId(offenderDiscordUser.Id);
            List<User?> players =
            [
                userRepository.GetByDiscordUser(discordUser1),
                userRepository.GetByDiscordUser(discordUser2),
                userRepository.GetByDiscordUser(discordUser3),
            ];

            if (offender is null ||
                players.Any(p => p is null))
            {
                await context.CreateResponseAsync("One or more users __not registered__.⚠️");
                return;
            }

            List<Match> matches = [.. matchRepository.Get(m =>
                    m._discordUserIds.Contains(offenderDiscordUser.Id.ToString()) &&
                    m._discordUserIds.Contains(discordUser1.Id.ToString()) &&
                    m._discordUserIds.Contains(discordUser2.Id.ToString()) &&
                    m._discordUserIds.Contains(discordUser3.Id.ToString()) &&
                    m.League == League.Pro &&
                    m.Season == DomainConfig.Season)
                .OrderByDescending(m => m.LastUpdatedAt)];

            foreach (Match? match in matches)
            {
                bool valid = match.GetSide(offenderDiscordUser) switch
                {
                    Match.Side.Team1 => players.All(p => match.GetSide(p!) == Match.Side.Team2),
                    Match.Side.Team2 => players.All(p => match.GetSide(p!) == Match.Side.Team1),
                    _ => false
                };

                if (!valid) continue;

                offender.Rating -= CoreConfig.Queue.ProDropPenalty;
                int compensation = CoreConfig.Queue.ProDropPenalty / 3;
                foreach (User? player in players) player!.Rating += compensation;
                await userRepository.SaveChangesAsync();

                string compensationInfo = players.Aggregate("", (current, user) => $"{current}`           [{user!.Rating}]` for {user.Mention} \n");
                string matchLink = match.HasJumpLink
                    ? match.JumpLink.DiscordLink($"{match.Id}")
                    : $"`{match.Id}`";
                string content = $"""
                               New Ratings (validated using {matchLink} {match.CreatedAt.DiscordTime(DiscordTimeFlag.R)}):
                               `-{CoreConfig.Queue.ProDropPenalty} | +{compensation} for {players.Count} players`
                               `(Offender) [{offender.Rating}]` for {offenderDiscordUser.Mention}
                               {compensationInfo}
                                               
                               reported by {context.User.Mention}
                               """;

                await context.CreateResponseAsync(content);
                await discordEngine.Log(content);
                return;
            }
        }

        [SlashCommand("Freeze", "Freeze user balance", false)]
        public async Task Freeze(InteractionContext context,
            [Option("User", "User")] DiscordUser discordUser,
            [Option("Amount", "How much mula")] double amount)
        {
            User? user = userRepository.GetByDiscordUser(discordUser); if (user is null) return;

            user.FreezeBalance(amount);

            await userRepository.SaveChangesAsync();

            await context.CreateResponseAsync($"{discordUser.Mention} `Balance: {user.AvailableBalance} Frozen: {user.Frozen}`");
        }

        [SlashCommand("Unfreeze", "Unfreeze user balance", false)]
        public async Task Unfreeze(InteractionContext context,
            [Option("User", "User")] DiscordUser discordUser,
            [Option("Amount", "How much mula")] double amount)
        {
            User? user = userRepository.GetByDiscordUser(discordUser); if (user is null) return;

            try
            {
                user.UnFreeze(amount);
            }
            catch (Exception e)
            {
                discordEngine.Log(e);
            }

            await userRepository.SaveChangesAsync();

            await context.CreateResponseAsync($"{discordUser.Mention} `Balance: {user.AvailableBalance} Frozen: {user.Frozen}`");
        }
    }
}
