using System.Diagnostics;

using BCL.Discord.Attributes.Permissions;
using BCL.Discord.Bot;
using BCL.Discord.Commands.SlashCommands.Users;
using BCL.Discord.Components.Dashboards;
using BCL.Discord.Utils;
using BCL.Domain;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

using DateTime = System.DateTime;

namespace BCL.Discord.Commands.SlashCommands.Admin;
public partial class AdminCommands
{
    [SlashCommand_Staff]
    [SlashCommandGroup("Membership", "Membership commands", false)]
    [SlashModuleLifespan(SlashModuleLifespan.Scoped)]
    public class Membership(IUserRepository userRepository, DiscordEngine discordEngine) : ApplicationCommandModule
    {
        [SlashCommand("DeclineProApplication", "Decline Pro Application")]
        public async Task DeclineProApplication(InteractionContext context,
            [Option("User", "User to time out from applying to pro")] DiscordUser discordUser,
            [Option("timeout", "How long is the timeout? (in days)")] Int64 timeout = 7)
        {
            await context.DeferAsync();

            User user = userRepository.GetByDiscordUser(discordUser) ?? throw new Exception($"User({discordUser.Id} | {discordUser.Username}) is not registered");

            user.ProApplicationTimeout = DateTime.UtcNow + TimeSpan.FromDays(timeout); //todo change back to days

            discordEngine.ProLeagueManager.Applications.TryGetValue(discordUser.Id, out Components.ProLeagueManager.Application? application);
            if (application?.Message != null)
            {
                await application.Message.DeleteAsync("Application completed");
                application.Message = null;
            }

            await userRepository.SaveChangesAsync();

            await context.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"{discordUser.Mention} timed out from applying to Pro league for {timeout} days"));
        }

        [SlashCommand("ChangeMembership", "Change a membership")]
        public async Task ChangeMembership(InteractionContext context,
            [Option("user", "Discord user you want to grant membership to.")] DiscordUser discordUser,
            [Option("pro", "True for pro false for noob")] bool pro = false)
        {
            await context.DeferAsync();

            User? user = userRepository.GetByDiscordId(discordUser.Id); if (user == null) { await UserCommands.SuggestRegistration(context, discordUser); return; }

            bool newMember = !user.Approved;
            user.Approved = true;

            string oldMembership = user.Pro ? "Pro" : "Standard";
            user.Pro = pro;
            user.ProQueue = pro;
            user.StandardQueue = !pro;
            string newMembership = pro ? "Pro" : "Standard";

            user.RegistrationInfo = $"{newMembership} by {context.User.Id} - {DateTime.UtcNow}";
            userRepository.SaveChanges();

            QueueTracker.MembersField.QueueInfos[newMembership].Count++;
            if (user.Vip) QueueTracker.MembersField.QueueInfos["Vip"].Count++; //revoked previously
            if (!newMember) QueueTracker.MembersField.QueueInfos[oldMembership].Count--;

            try
            {
                ulong regionRoleId = user.Server switch
                {
                    Region.Eu => DiscordConfig.Roles.Region.EuId,
                    Region.Na => DiscordConfig.Roles.Region.NaId,
                    Region.Sa => DiscordConfig.Roles.Region.SaId,

                    Region.Unknown => throw new NotImplementedException(),
                    _ => throw new UnreachableException()
                };

                DiscordMember member = context.Guild.Members[user.DiscordId];
                await member.GrantRoleAsync(context.Guild.Roles[DiscordConfig.Roles.MemberId]);
                await member.GrantRoleAsync(context.Guild.Roles[regionRoleId]);

                if (pro)
                {
                    discordEngine.ProLeagueManager.Applications.TryGetValue(discordUser.Id, out Components.ProLeagueManager.Application? application);
                    if (application?.Message != null)
                    {
                        await application.Message.DeleteAsync("Application completed");
                        application.Message = null;
                        discordEngine.ProLeagueManager.Applications.Remove(application.DiscordId);
                    }

                    await member.GrantRoleAsync(context.Guild.Roles[DiscordConfig.Roles.ProId]);
                    await member.GrantRoleAsync(context.Guild.Roles[DiscordConfig.Roles.QueuesProId]);
                    await member.RevokeRoleAsync(context.Guild.Roles[DiscordConfig.Roles.QueuesStandardId]);

                    user.CurrentSeasonStats.Membership = League.Pro;
                }
                else
                {
                    await member.RevokeRoleAsync(context.Guild.Roles[DiscordConfig.Roles.ProId]);
                    await member.RevokeRoleAsync(context.Guild.Roles[DiscordConfig.Roles.QueuesProId]);
                    await member.GrantRoleAsync(context.Guild.Roles[DiscordConfig.Roles.QueuesStandardId]);

                    user.CurrentSeasonStats.Membership = League.Standard;
                }
            }
            catch (Exception e)
            {
                await discordEngine.Log(e);
            }

            string content = $"{(pro ? "🐐" : "🐕")}`{user.InGameName}` ; {user.Mention}'s membership set to `{newMembership}` by {context.User.Mention}.";
            await discordEngine.Log(content);
            await context.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(content)
                    .AddMention(new UserMention(discordUser)));
        }

        [SlashCommand("Vip", "Modify vip status of a bcl supporter")]
        public async Task Vip(InteractionContext context,
            [Option("DiscordUser", "Discord user")] DiscordUser discordUser,
            [Option("Value", "true for yes false for no :^)")] bool value)
        {
            User? user = userRepository.GetByDiscordId(discordUser.Id); if (user == null) { await UserCommands.SuggestRegistration(context, discordUser); return; }

            if (user.Vip != value)
            {
                user.Vip = value;
                switch (value)
                {
                    case true: QueueTracker.MembersField.QueueInfos["Vip"].Count++; break;
                    case false: QueueTracker.MembersField.QueueInfos["Vip"].Count--; break;
                }

                userRepository.SaveChanges();
            }

            DiscordMember member = context.Guild.Members[discordUser.Id];
            switch (value)
            {
                case true: await member.GrantRoleAsync(context.Guild.Roles[DiscordConfig.Roles.SupporterId]); break;
                case false: await member.RevokeRoleAsync(context.Guild.Roles[DiscordConfig.Roles.SupporterId]); break;
            }

            string content = $"{user.Mention}'s supporter status set to `{user.Vip}` by {context.User.Mention}";
            await context.CreateResponseAsync(content);
            await discordEngine.Log(content);
        }

        [SlashCommand("Revoke", "Revokes a user's permission to queue BCL.")]
        public async Task Revoke(InteractionContext context,
            [Option("user", "Discord user.")] DiscordUser? discordUser = null,
            [Option("inGameName", "Player")] string? inGameName = null)
        {
            (User? user, DiscordUser? _) = await UserUtils.GetUser(context, userRepository, discordUser, inGameName);
            if (user is null) { await context.CreateResponseAsync("User not found."); return; }

            bool wasApproved = user.Approved;
            user.Approved = false;
            user.RegistrationInfo = $"Revoked by {context.User.Id} - {DateTime.UtcNow}";

            if (wasApproved)
            {
                QueueTracker.MembersField.QueueInfos[user.Server.ToString()].Count--;
                if (user.Vip) QueueTracker.MembersField.QueueInfos["Vip"].Count--;
                if (user.Pro) QueueTracker.MembersField.QueueInfos["Pro"].Count--;
                else QueueTracker.MembersField.QueueInfos["Standard"].Count--;
            }

            userRepository.SaveChanges();
            try
            {
                DiscordMember member = context.Guild.Members[user.DiscordId];
                await member.RevokeRoleAsync(context.Guild.Roles[DiscordConfig.Roles.MemberId]);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            string content = $"⚠️`{user.InGameName}` ; {user.Mention}'s membership revoked by {context.User.Mention}.";
            await discordEngine.Log(content);
            await context.CreateResponseAsync(content);
        }

        [SlashCommand("AdjustMmr", "Manually set someone's mmr and placement games")]
        public async Task AdjustMmr(InteractionContext context,
            [Option("DiscordUser", "Who's gonna be your next nephew")] DiscordUser discordUser,
            [Option("Rating", "What rating")] double rating,
            [Option("RatingStandard", "What rating")] double ratingStandard,
            [Option("PlacementGames", "How many placement games?")] Int64 placements = -1,
            [Option("PlacementGamesStandard", "How many placement games?")] Int64 placementsStandard = -1)
        {
            User? user = userRepository.GetByDiscordId(discordUser.Id); if (user is null) { await UserCommands.SuggestRegistration(context, discordUser); return; }

            if (context.User.Id == DiscordConfig.DevId)
            {
                user.Rating = rating;
                if (placements != -1) user.PlacementGamesRemaining = unchecked((int)placements);
            }

            user.Rating_Standard = ratingStandard;
            if (placementsStandard != -1) user.PlacementGamesRemaining_Standard = unchecked((int)placementsStandard);

            await userRepository.SaveChangesAsync();

            string content = $"{discordUser.Mention}'s MMR set to `{user.Rating} Pro` | `{user.Rating_Standard} Standard` and placement games remaining to `{user.PlacementGamesRemaining} Pro` | `{user.PlacementGamesRemaining_Standard} Standard`";
            await context.CreateResponseAsync(content);
            await discordEngine.Log(content);
        }

        [SlashCommand("UserInfo", "Returns the user info")]
        public async Task UserInfo(InteractionContext context,
            [Option("user", "Discord user.")] DiscordUser? discordUser = null,
            [Option("inGameName", "Player")] string? inGameName = null)
        {
            (User? user, DiscordUser? _) = await UserUtils.GetUser(context, userRepository, discordUser, inGameName);
            if (user is null) { await context.CreateResponseAsync("User not found."); return; }

            await context.CreateResponseAsync($"""

                                               Name: {user.InGameName} | {user.Mention} | `{user.DiscordId}`
                                               Id: {user.Id}
                                               Placements Standard: {user.PlacementGamesRemaining_Standard}
                                               Placements Pro: {user.PlacementGamesRemaining}
                                               New match DM: {user.NewMatchDm}
                                               Profile version: {user.ProfileVersion}
                                               Created at: {user.CreatedAt}
                                               Last updated at: {user.LastUpdatedAt}
                                               Registered by: {user.RegistrationInfo}
                                               """);
        }

        [SlashCommand("CrossRegion", "Change a user's Cross Region Flag.")]
        public async Task CrossRegion(InteractionContext context,
            [Option("DiscordUser", "Discord User")] DiscordUser discordUser,
            [Option("value", "value")] bool value)
        {
            User? user = userRepository.GetByDiscordId(discordUser.Id);
            if (user == null) return;

            user.CrossRegion = value;
            await userRepository.SaveChangesAsync();

            string content = $"{discordUser.Mention}'s cross region flag set to `{value}` by {context.User.Mention}.";
            await discordEngine.Log(content);
            await context.CreateResponseAsync(content);
        }

        [SlashCommand("ResetSupporterSettings", "Unlucky")]
        public async Task ResetSupporterSettings(InteractionContext context,
            [Option("user", "user")] DiscordUser discordUser)
        {
            User? user = userRepository.GetByDiscordId(discordUser.Id);
            if (user is null) return;

            string oldValues = $"""

                             Roles {user.RoleMention} {user.SecondaryRoleMention}
                             > name`{user.RoleName}`
                             > suffix`{user.RoleSuffix}`
                             > color`{user.RoleColor}`
                             > url`{user.RoleIconUrl}`
                             Channel {user.ChannelMention}
                             > name`{user.ChannelName}`
                             Emoji`{user.EmojiId}`
                             Profile
                             > teamName`{user.TeamName}`
                             > chartAlpha`{user.ChartAlpha}`
                             > color`{user.ProfileColor}`
                             """;

            #region Try Delete Existing Snowflakes LUL

            List<Exception> exceptions = [];
            try
            {
                if (user.RoleId is not 0)
                    await context.Guild.Roles[user.RoleId].DeleteAsync();
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
            try
            {
                if (user.SecondaryRoleId is not 0)
                    await context.Guild.Roles[user.SecondaryRoleId].DeleteAsync();
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
            try
            {
                if (user.ChannelId is not 0)
                    await context.Guild.Channels[user.ChannelId].DeleteAsync();
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
            try
            {
                if (user.EmojiId is not 0)
                    await context.Guild.DeleteEmojiAsync(await context.Guild.GetEmojiAsync(user.EmojiId));
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }

            string errors = exceptions.Count > 0
                ? $"""
                   Errors:
                   ```csharp
                   {exceptions.Aggregate("", (current, ex) => $"{current} {ex.Message}\n")}
                   ```
                   """
                : string.Empty;

            #endregion

            user.RoleId = default;
            user.SecondaryRoleId = default;
            user.ChannelId = default;
            user.EmojiId = default;
            user.RoleIconUrl += " ";
            user.TeamName = string.Empty;
            user.ChartAlpha = DomainConfig.Profile.DefaultChartAlpha;
            user.ProfileColor = DomainConfig.Profile.DefaultColor;

            await userRepository.SaveChangesAsync();
            string content = $"Reset discord-bound supporter settings for {discordUser.Mention}{errors}";
            await context.CreateResponseAsync($"{content}\n{oldValues}");
            await discordEngine.Log(content);
        }
    }
}
