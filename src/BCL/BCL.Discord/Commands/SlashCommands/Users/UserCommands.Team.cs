//using BCL.Core;
//using BCL.Core.Services.Abstract;
//using BCL.Discord.Attributes.Permissions;
//using BCL.Discord.Bot;
//using BCL.Discord.Components.Dashboards;
//using BCL.Discord.Extensions;
//using BCL.Discord.OptionProviders;
//using BCL.Domain;
//using BCL.Domain.Dtos;
//using BCL.Domain.Entities.Users;
//using BCL.Domain.Enums;
//using BCL.Persistence.Sqlite.Repositories.Abstract;
//using DSharpPlus.Entities;
//using DSharpPlus.Interactivity.Extensions;
//using DSharpPlus.SlashCommands;

//namespace BCL.Discord.Commands.SlashCommands.Users;
//public partial class UserCommands
//{
//    [SlashCommandGroup("Team", "Team utilities.")]
//    [SlashModuleLifespan(SlashModuleLifespan.Scoped)]
//    public class Team : ApplicationCommandModule
//    {
//        private readonly IUserRepository _userRepository;
//        private readonly ITeamRepository _teamRepository;
//        private readonly IQueueService _queueService;
//        private readonly IMatchmakingService _matchmakingService;
//        private readonly IMapRepository _mapRepository;
//        private readonly DiscordEngine _discordEngine;

//        public Team(
//            IUserRepository userRepository,
//            ITeamRepository teamRepository,
//            IQueueService queueService,
//            IMatchmakingService matchmakingService,
//            IMapRepository mapRepository,
//            DiscordEngine discordEngine
//        )
//        {
//            _userRepository = userRepository;
//            _teamRepository = teamRepository;
//            _queueService = queueService;
//            _matchmakingService = matchmakingService;
//            _mapRepository = mapRepository;
//            _discordEngine = discordEngine;
//        }

//        //TODO Add select after multiple teams are supported

//        [SlashCommand("Create", "Create a team.")]
//        public async Task Create(InteractionContext context,
//            [Option("Name", "Team name go here")] string name
//            //[Option("Size", "Number of players in team.")] int size = 3
//            )
//        {
//            var size = 3; //todo implement dynamic team size
//            await context.DeferAsync();

//            var user = _userRepository.GetByDiscordId(context.User.Id);
//            if (user == null || user.Approved is false) throw new Exception($"You need to be a {DomainConfig.ServerAlias} member to create a team.");
//            if (_teamRepository.Get(t => t.Name == name).Any()) throw new Exception($"Team name `{name}` in use.");
//            if (string.IsNullOrEmpty(name)) throw new Exception("Team name can not be empty");
//            if (PremadeTeam.TeamSizes.All(ts => ts != size)) throw new Exception("Invalid team size.");

//            var team = new PremadeTeam
//            {
//                Name = name,
//                Size = size,

//            };
//            team.Members.Add(user);

//            user.TeamId = (await _teamRepository.AddAsync(team)).Entity.Id;

//            await _teamRepository.SaveChangesAsync();
//            await context.EditResponseAsync(new DiscordWebhookBuilder()
//                .WithContent($"Team `{team.Name}` created successfully. Invite some players!({team.Members.Count}/{team.Size}) :+1:"));
//        }

//        private readonly SemaphoreSlim _gate = new(1);

//        [SlashCommand("Invite", "Invite a player to your active team.")]
//        public async Task Invite(InteractionContext context,
//            [Option("Member", "Member you want to invite")] DiscordUser invitedMember)
//        {
//            await context.DeferAsync();

//            var captain = _userRepository.GetByDiscordUser(context.User);
//            if (captain is null || captain.Approved is false || captain.TeamId == Ulid.Empty) throw new Exception("You need own your active team to use this command");

//            var team = _teamRepository.GetById(captain.TeamId);
//            if (team is null || team.Captain.Id != captain.Id) throw new Exception("You need to be the captain of your active team to use this command.");

//            var newTeamMember = _userRepository.GetByDiscordUser(invitedMember) ?? throw new Exception($"{invitedMember.Mention} is not registered.");

//            #region Confirmation

//            var embed = new DiscordEmbedBuilder()
//                    .WithAuthor(newTeamMember.InGameName, null, invitedMember.AvatarUrl)
//                    .WithDescription($"""
//                                      {newTeamMember.Mention} you have been invited to join the `{team.Name}` team by {captain.Mention}.
//                                      React with :white_check_mark: to accept or :x: to decline
//                                      """);

//            foreach (var member in team.Members)
//                embed.AddField(member.InGameName, $"Pro: `{(member.Pro ? member.Rating.FormatForDiscordCode(4) : "----")}` | Standard: `{member.Rating_Standard.FormatForDiscordCode(4)}` | Activity: `{member.LatestSnapshot?.GamesPlayed.FormatForDiscordCode(4)}` matches | {member.Mention}");

//            var confirmationPromptMessageBuilder = new DiscordMessageBuilder()
//                            .AddEmbed(embed)
//                            .WithContent($"{newTeamMember.Mention}.")
//                            .WithAllowedMention(new UserMention(newTeamMember.DiscordId));
//            var confirmationPrompt = await confirmationPromptMessageBuilder
//                .SendAsync(_discordEngine.QueueChannel);

//            var confirmEmoji = DiscordEmoji.FromName(context.Client, ":white_check_mark:");
//            var declineEmoji = DiscordEmoji.FromName(context.Client, ":x:");

//#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
//            confirmationPrompt.CreateReactionAsync(confirmEmoji);
//            confirmationPrompt.CreateReactionAsync(declineEmoji);

//            var queueTrackerMessage = new QueueTracker.QueueTrackerMessage(
//                            confirmationPromptMessageBuilder,
//                            confirmationPrompt,
//                            DateTime.UtcNow.AddMinutes(15));
//            _discordEngine.QueueTracker.DoNotPurge.Add(queueTrackerMessage);

//            var interactivity = context.Client.GetInteractivity();
//            var validReaction = false;
//            while (!validReaction)
//            {
//                var response = await interactivity.WaitForReactionAsync(r =>
//                    (r.Message.Id == confirmationPrompt.Id
//                     && r.User.Id == invitedMember.Id), TimeSpan.FromMinutes(2));

//                if (response.TimedOut || response.Result.Emoji == declineEmoji)
//                {
//                    confirmationPrompt.DeleteAsync();
//                    throw new Exception(response.TimedOut ? "Invitation timed out" : "Invitation Declined");
//                }
//                if (response.Result.Emoji == confirmEmoji) validReaction = true;
//            }

//            _discordEngine.QueueTracker.DoNotPurge.Remove(queueTrackerMessage);
//            confirmationPrompt.DeleteAsync();
//            context.EditResponseAsync(new DiscordWebhookBuilder().WithContent(":white_check_mark: Accepted"));
//#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
//            #endregion

//            await _gate.WaitAsync(10000);

//            team = _teamRepository.GetById(team.Id);
//            if (team!.Members.Count >= team.Size) throw new Exception("Team is full");

//            newTeamMember.TeamId = team.Id;
//            team.Members.Add(newTeamMember);
//            team.MemberIds = team.Members.Select(m => m.DiscordId).Aggregate("",
//                (current, next) => $"{current}|{next}").Trim('|');

//            if (team.Members.Count == team.Size)
//            {
//                team.Rating = Math.Floor(team.Members
//                    .Select(u => u.Pro ? u.Rating : (u.Rating_Standard - 200))
//                    .Average());
//                team.IsRated = true;
//            }

//            _teamRepository.SaveChanges();

//            var discordEmbedBuilder = new DiscordEmbedBuilder()
//                .WithDescription($"{newTeamMember.Mention} joined `{team.Name}`.");

//            foreach (var member in team.Members)
//                discordEmbedBuilder.AddField(member.InGameName, $"Pro: `{(member.Pro ? member.Rating.FormatForDiscordCode(4) : "----")}` | Standard: `{member.Rating_Standard.FormatForDiscordCode(4)}` | Activity: `{member.LatestSnapshot?.GamesPlayed.FormatForDiscordCode(4)}` matches | {member.Mention}");

//            await context.FollowUpAsync(new DiscordFollowupMessageBuilder()
//                .AddEmbed(discordEmbedBuilder));

//            _gate.Release();
//        }

//        [SlashCommand("Leave", "Leave a team.")]
//        public async Task Leave(InteractionContext context)
//        {
//            await context.DeferAsync();
//            var user = _userRepository.GetByDiscordUser(context.User) ?? throw new Exception("User is not registered");
//            if (user.TeamId == Ulid.Empty) throw new Exception("User has no active team");
//            var team = _teamRepository.GetById(user.TeamId) ?? throw new Exception($"Team with id '{user.TeamId}' does not exist");

//            user.TeamId = Ulid.Empty;
//            team.Members.RemoveAll(m => m.Id == user.Id);
//            team.MemberIds = team.MemberIds.Replace(user.Id.ToString()!, string.Empty).Trim('|'); //todo meh

//            await _teamRepository.SaveChangesAsync();

//            await context.EditResponseAsync(new DiscordWebhookBuilder()
//                .WithContent($"{user.Mention} successfully left team `{team.Name}`"));
//        }

//        [SlashCommand("SetActiveTeam", "Select your active team")]
//        public async Task SetActiveTeam(InteractionContext context)
//        {
//            await context.DeferAsync();

//            var user = _userRepository.GetByDiscordUser(context.User) ?? throw new Exception("User is not registered");
//            if (_queueService.IsInPremadeQueue(context.User, out _))
//                throw new Exception("Can not change active team while in premade queue");

//            var teams = _teamRepository.GetByDiscordUser(context.User).ToList();
//            if (!teams.Any()) throw new Exception("User is not part of any teams");

//            var interactivity = context.Client.GetInteractivity();

//            var content = "Select your active team:";
//            var index = 0;
//            foreach (var team in teams)
//            {
//                content += $"\n{index}. `{team.Name}`";
//                index++;
//            }

//            content += "> \n*Write the index in chat (-1 if you want to have no active team, useful if you want to create a new team)*";

//            await context.EditResponseAsync(new DiscordWebhookBuilder()
//                .WithContent(content));

//            Ulid? selectedTeamId;
//            while (true)
//            {
//                var response = await interactivity.WaitForMessageAsync(reply =>
//                     reply.Author.Id == context.User.Id, TimeSpan.FromMinutes(1));

//                if (response.TimedOut) throw new Exception("Interaction timed out");

//                //todo replace with index lookup

//                if (!int.TryParse(response.Result.Content, out var selectedTeamIndex)
//                    || selectedTeamIndex < -1 || selectedTeamIndex >= teams.Count - 1)
//                    continue;

//                selectedTeamId = selectedTeamIndex == -1 
//                    ? null
//                    : teams[selectedTeamIndex].Id;

//                break;
//            }

//            if (_queueService.IsUserInQueue(context.User))
//            {

//                throw new Exception("You can not change your active team while in queue.");
//                //await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You can not change your active team while in queue."));
//                //return;
//            }

//            user.TeamId = selectedTeamId ?? Ulid.Empty;
//            _userRepository.SaveChanges();

//            await context.EditResponseAsync(new DiscordWebhookBuilder()
//                .WithContent(selectedTeamId is null 
//                    ? "You don't have an active team."
//                    : $"Your active team is `{teams.Single(t => t.Id == selectedTeamId).Name}`."));

//        }

//        [SlashCommand("ViewTeamMembers", "View your team members")]
//        public async Task ViewTeamMembers(InteractionContext context)
//        {
//            await context.DeferAsync();

//            var teams = _teamRepository.GetByDiscordUser(context.User).ToList();
//            if (!teams.Any()) throw new Exception("User is not part of any teams");

//            var interactivity = context.Client.GetInteractivity();

//            var content = "Select a team:";
//            var index = 0;
//            foreach (var team in teams)
//            {
//                content += $"\n{index}. `{team.Name}` | `{team.Rating}`";
//                index++;
//            }

//            content += "\n> *Write the index in chat*";

//            await context.EditResponseAsync(new DiscordWebhookBuilder()
//                .WithContent(content));

//            Ulid selectedTeamId;
//            while (true)
//            {
//                var response = await interactivity.WaitForMessageAsync(reply =>
//                    reply.Author.Id == context.User.Id, TimeSpan.FromMinutes(1));

//                if (response.TimedOut) throw new Exception("Interaction timed out");

//                //todo replace with index lookup

//                if (!int.TryParse(response.Result.Content.Trim(), out var selectedTeamIndex)
//                    || selectedTeamIndex < 0 || selectedTeamIndex > teams.Count)
//                    continue;

//                selectedTeamId = teams[selectedTeamIndex - 1].Id;

//                break;
//            }

//            var selectedTeam = teams.First(t => t.Id == selectedTeamId);
//            var mentions = selectedTeam.Members.Aggregate("", (current, next) => $"{current} {next.Mention}");

//            await context.EditResponseAsync(new DiscordWebhookBuilder()
//                .WithContent($"Your team is {mentions} | Rating: {selectedTeam.Rating}"));

//        }

//        [SlashCommand("Remove", "Remove player from your team.")]
//        public async Task Remove(InteractionContext context,
//            [Option("Member", "Member you want to remove")] DiscordUser member)
//        {
//            await context.DeferAsync();

//            var captain = _userRepository.GetByDiscordUser(context.User);
//            if (captain is null || captain.Approved is false || captain.TeamId == Ulid.Empty)
//                throw new Exception("User does not have an active team");

//            var team = _teamRepository.GetById(captain.TeamId);
//            if (team is null || team.Captain.Id != captain.Id)
//                throw new Exception("User is not the team captain or team doesn't exist");

//            var memberToRemove = _userRepository.GetByDiscordUser(member);
//            if (memberToRemove is null || memberToRemove.TeamId != team.Id)
//                throw new Exception("Target user is not part of the team");

//            memberToRemove.TeamId = Ulid.Empty;
//            team.Members.RemoveAll(m => m.Id == memberToRemove.Id);
//            team.MemberIds = team.MemberIds.Replace(memberToRemove.Id.ToString()!, string.Empty).Trim('|'); //todo meh
//            _teamRepository.SaveChanges();

//            await context.EditResponseAsync(new DiscordWebhookBuilder()
//                .WithContent($"{memberToRemove.Mention} successfully left team `{team.Name}`"));
//        }

//        [SlashCommand_Supporter]
//        [SlashCommand("Scrim", "Invite another team to scrim")]
//        public async Task Scrim(InteractionContext context,
//            [Option("Opponent", "Who you are inviting to scrim your active team")] DiscordUser opponent,
//            [Option("DraftType", "Type of draft you want to use")] DraftType draftType,
//            [Option("Region", "Region")] Region region,
//            [ChoiceProvider(typeof(MapChoiceProvider))]
//            [Option("Map", "Game map")] string? mapId = null)
//        {
//            await context.CreateResponseAsync($"Inviting {opponent.Mention} to Scrim Match...");

//            var team1User = _userRepository.GetByDiscordUser(context.User);
//            var team2User = _userRepository.GetByDiscordUser(opponent);

//            if (team1User is null || team1User.TeamId == Ulid.Empty ||
//                team2User is null || team2User.TeamId == Ulid.Empty ||
//                team1User.TeamId == team2User.TeamId) throw new Exception("Invalid Teams");

//            var team1 = _teamRepository.GetById(team1User.TeamId);
//            var team2 = _teamRepository.GetById(team2User.TeamId);

//            if (team1 is null ||
//                team2 is null ||
//                team1.Size != team2.Size ||
//                team1.Valid is false ||
//                team2.Valid is false) throw new Exception("Invalid Teams");

//            #region Confirmation

//            var mentions = team2.Members.Aggregate("", (current, next) => $"{current} {next.Mention}");

//            var embed = new DiscordEmbedBuilder()
//                    .WithAuthor(team1User.InGameName, null, context.User.AvatarUrl)
//                    .WithDescription($"""
//                                      {mentions} you have been invited to scrim {context.User.Mention}'s Team `{team1.Name}`.
//                                      React with :white_check_mark: to accept or :x: to decline
//                                      """);

//            embed.AddField($"{team1.Name}",
//                team1.Members.Aggregate("", (current, next) => $"{current}\n{next}"));

//            embed.AddField($"{team2.Name}",
//                team2.Members.Aggregate("", (current, next) => $"{current}\n{next}"));

//            var message = new DiscordMessageBuilder()
//                .AddEmbed(embed)
//                .WithContent($"{opponent.Mention}.")
//                .WithAllowedMention(new UserMention(opponent.Id));

//            var confirmationPrompt = await message.SendAsync(_discordEngine.QueueChannel);

//            var confirmEmoji = DiscordEmoji.FromName(context.Client, ":white_check_mark:");
//            var declineEmoji = DiscordEmoji.FromName(context.Client, ":x:");

//            await confirmationPrompt.CreateReactionAsync(confirmEmoji);
//            await confirmationPrompt.CreateReactionAsync(declineEmoji);

//            var queueTrackerMessage = new QueueTracker.QueueTrackerMessage(
//                            message,
//                            confirmationPrompt,
//                            DateTime.UtcNow.AddMinutes(15));
//            _discordEngine.QueueTracker.DoNotPurge.Add(queueTrackerMessage);

//            var interactivity = context.Client.GetInteractivity();
//            var validReaction = false;
//            while (!validReaction)
//            {
//                var response = await interactivity.WaitForReactionAsync(r =>
//                    (r.Message.Id == confirmationPrompt.Id &&
//                    team2.Members.Any(m => m.DiscordId == r.User.Id)),
//                    TimeSpan.FromMinutes(2));

//                if (response.TimedOut || response.Result.Emoji == declineEmoji)
//                {
//                    await confirmationPrompt.DeleteAsync();
//                    await context.EditResponseAsync(new DiscordWebhookBuilder()
//                        .WithContent($":x: Declined{(response.TimedOut ? " (Timed out...)" : "")}"));
//                    return;
//                }
//                if (response.Result.Emoji == confirmEmoji) validReaction = true;
//            }

//            _discordEngine.QueueTracker.DoNotPurge.Remove(queueTrackerMessage);
//            await confirmationPrompt.DeleteAsync();
//            await context.EditResponseAsync(new DiscordWebhookBuilder()
//                .WithContent(":white_check_mark: Accepted"));

//            #endregion

//            var draftFormat = CoreConfig.Draft.ProFormatRaw;
//            var format = draftFormat.Split('-').ToList();

//            foreach (var member in team1.Members)
//            {
//                if (member.TeamId != team1.Id)
//                {
//                    await context.EditResponseAsync(new DiscordWebhookBuilder()
//                        .WithContent($"{member.Mention} does not have __{team1.Name}__ as their active team."));
//                    return;
//                }
//            }

//            foreach (var member in team2.Members)
//            {
//                if (member.TeamId != team2.Id)
//                {
//                    await context.EditResponseAsync(new DiscordWebhookBuilder()
//                        .WithContent($"{member.Mention} does not have __{team2.Name}__ as their active team."));
//                    return;
//                }
//            }

//            var map = mapId is not null ? await _mapRepository.GetByIdAsync(mapId) : null;

//            var match = await _matchmakingService.CreateMatch(
//                team1.Members.Concat(team2.Members)
//                    .Select(u => new Player(u)).ToList(),
//                region,
//                League.Custom,
//                format,
//                MatchmakingLogic.None,
//                map,
//                team1.Name,
//                team2.Name,
//                draftType);

//            await context.EditResponseAsync(new DiscordWebhookBuilder()
//                .WithContent($"**{League.Custom}** match created with __{draftType}__ draft, `{draftFormat}` format and id `{match.Id}`"));
//        }

//    }
//}
