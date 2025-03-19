using System.Linq.Expressions;
using System.Text.RegularExpressions;

using BCL.Core;
using BCL.Core.Services.Abstract;
using BCL.Discord.Bot;
using BCL.Discord.Commands.ButtonCommands;
using BCL.Discord.Commands.SlashCommands;
using BCL.Discord.Commands.SlashCommands.Users;
using BCL.Discord.Components.Dashboards;
using BCL.Discord.Extensions;
using BCL.Domain;
using BCL.Domain.Entities.Analytics;
using BCL.Domain.Entities.Queue;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus;
using DSharpPlus.ButtonCommands.Extensions;
using DSharpPlus.Entities;
using DSharpPlus.ModalCommands;
using DSharpPlus.ModalCommands.Attributes;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

#pragma warning disable CS4014

namespace BCL.Discord.Commands.ModalCommands;
public partial class UserModals(
    IUserRepository userRepository,
    IChampionRepository championRepository,
    HttpClient httpClient,
    DiscordEngine discordEngine,
    IQueueService queue) : ModalCommandModule
{
    private readonly string[] _illegalSequences = ["|", " ", ",", ";", "/", @"\", "<@", "<@&", "<#", ">"];
    [ModalCommand("Register")]
    public async Task Register(ModalContext context, string inGameName, int selfRating, string server, string melee, string ranged, string support)
    {
        inGameName = _illegalSequences.Aggregate(inGameName, (current, sequence) => current.Replace(sequence, "_")).Trim();
        await context.CreateResponseAsync("Registering...");

        (string? defaultMelee, string? defaultRanged, string? defaultSupport) = ValidateChampionPool(melee, ranged, support, championRepository);
        if (string.IsNullOrWhiteSpace(defaultMelee) && string.IsNullOrWhiteSpace(defaultRanged) && string.IsNullOrWhiteSpace(defaultSupport))
        {
            context.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"""
                                                                               {context.User.Mention} Please provide champion pool information for at least one role.
                                                                               Valid values are __Champion names__, __unique abreviations__ or "ALL".
                                                                               Valid Separators are `, ; /` or a new line.
                                                                               """)
                .AddMention(new UserMention(context.User)));
            return;
        }

        if (selfRating < 1) selfRating = 1;
        if (selfRating > 10) selfRating = 10;
        selfRating *= 100;

        User user = new()
        {
            Name = context.User.Username,
            DiscordId = context.User.Id,
            InGameName = inGameName,
            Server = server.ToLower() switch
            {
                "eu" => Region.Eu,
                "na" => Region.Na,
                "sa" => Region.Sa,
                _ => Region.Unknown
            },
            DefaultMelee = defaultMelee ?? "None",
            DefaultRanged = defaultRanged ?? "None",
            DefaultSupport = defaultSupport ?? "None",
            Rating_Standard = selfRating,
            PlacementGamesRemaining = CoreConfig.Queue.PlacementGames,
            PlacementGamesRemaining_Standard = CoreConfig.Queue.PlacementGames,
            StandardQueue = true,
            SeasonStats =
            [
                new Stats
                {
                    LowestRating_Standard = selfRating,
                    Season = DomainConfig.Season,
                    Snapshots =
                    [
                        new StatsSnapshot
                        {
                            Rating = DomainConfig.DefaultRating,
                            Rating_Standard = selfRating
                        }
                    ]
                }
            ]
        };

        //Roles
        user.Eu = user.Server == Region.Eu;
        user.Na = user.Server == Region.Na;
        user.Sa = user.Server == Region.Sa;
        if (user.Eu)
        {
            context.Member!.GrantRoleAsync(discordEngine.Guild.Roles[DiscordConfig.Roles.QueuesEuId]);
        }
        if (user.Na)
        {
            context.Member!.GrantRoleAsync(discordEngine.Guild.Roles[DiscordConfig.Roles.QueuesNaId]);
        }
        if (user.Sa)
        {
            context.Member!.GrantRoleAsync(discordEngine.Guild.Roles[DiscordConfig.Roles.QueuesSaId]);
        }
        context.Member!.GrantRoleAsync(discordEngine.Guild.Roles[DiscordConfig.Roles.QueuesStandardId]);

        await SendRegistrationApplication(context, user);
        string settingsCommand = context.Client.MentionCommand<UserCommands>(nameof(UserCommands.Settings));
        context.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                $"Successfully registered. {context.User.Mention} To update your profile use {settingsCommand}")
            .AddMention(new UserMention(context.User)));

        await userRepository.AddAsync(user);
        await userRepository.SaveChangesAsync();
    }

    [ModalCommand("ProfileSettings")]
    public async Task ProfileSettings(ModalContext context, Ulid userId, string inGameName, string server, string melee, string ranged, string support)
    {
        await context.CreateResponseAsync("Updating **profile** settings...", true);

        inGameName = _illegalSequences.Aggregate(inGameName, (current, sequence) => current.Replace(sequence, "_")).Trim();
        User? user = await userRepository.GetByIdAsync(userId); if (user == null) return;
        if (user.InGameName != inGameName)
        {
            User? checkIgn = userRepository.GetByIgn(inGameName);
            if (checkIgn is not null && checkIgn.Id != user.Id)
            {
                await context.CreateResponseAsync($"{context.User.Mention} InGameName `{inGameName}` in use by {checkIgn.Mention}. Please contact staff if your name has been stolen.");
                return;
            }
        }

        (string? defaultMelee, string? defaultRanged, string? defaultSupport) = ValidateChampionPool(melee, ranged, support, championRepository);
        if (string.IsNullOrWhiteSpace(defaultMelee) && string.IsNullOrWhiteSpace(defaultRanged) && string.IsNullOrWhiteSpace(defaultSupport))
        {
            context.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"""
                                                                               {context.User.Mention} Please provide champion pool information for at least one role.
                                                                               Valid values are __Champion names__, __unique abreviations__ or "ALL".
                                                                               Valid Separators are `, ; /` or a new line.
                                                                               """)
                .AddMention(new UserMention(context.User)));
            return;
        }

        if (user.DiscordId == context.User.Id) user.Name = context.User.Username;
        user.InGameName = inGameName;
        Region newServer = server.ToLower() switch
        {
            "eu" => Region.Eu,
            "na" => Region.Na,
            "sa" => Region.Sa,
            _ => Region.Unknown
        };

        user.CurrentSeasonStats.Membership = user.Approved switch
        {
            true when user.Pro => League.Pro,
            true when !user.Pro => League.Standard,
            _ => League.Custom
        };

        if (user.Server != newServer)
        {
            QueueTracker.MembersField.QueueInfos[user.Server.ToString()].Count--;
            QueueTracker.MembersField.QueueInfos[newServer.ToString()].Count++;
        }

        user.Server = newServer;
        user.DefaultMelee = defaultMelee ?? "None";
        user.DefaultRanged = defaultRanged ?? "None";
        user.DefaultSupport = defaultSupport ?? "None";
        user.ProfileVersion = DomainConfig.Profile.Version;

        DiscordMember discordMember = discordEngine.Guild.Members[user.DiscordId];
        if (user.Approved) discordMember.GrantRoleAsync(discordEngine.Guild.Roles[DiscordConfig.Roles.MemberId]);

        context.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
            $"{discordMember.Mention}'s profile updated{(user.DiscordId != context.User.Id ? $" by {context.User.Mention}" : string.Empty)}."));

        await userRepository.SaveChangesAsync();

        context.Interaction.DeleteOriginalResponseAsync(10);
    }

    [ModalCommand("QueueSettings")]
    public async Task QueueSettings(ModalContext context, Ulid userId, int purge, bool newMatchDm)
    {
        await context.CreateResponseAsync("Updating **queue** settings...", true);

        User? user = await userRepository.GetByIdAsync(userId); if (user == null) return;
        user.PurgeAfter = purge;
        user.NewMatchDm = newMatchDm;

        if (queue.IsUserInQueue(user))
        {
            QueueRole role = queue.CurrentRole(user.DiscordId);
            queue.Leave(user.DiscordId);
            QueueCommands.ClearPurgeJob(context.User.Id);
            QueueCommands._JoinQueue(context.Interaction, context.Client, userRepository, queue, discordEngine, role, false);
        }

        context.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
            $"{user.Mention}'s queue settings updated{(user.DiscordId != context.User.Id ? $" by {context.User.Mention}" : string.Empty)}."));

        await userRepository.SaveChangesAsync();

        context.Interaction.DeleteOriginalResponseAsync(10);
    }

    //[ModalCommand("CurrencySettings")]
    //public async Task CurrencySettings(ModalContext context, Ulid userId, double betAmount = 0)
    //{
    //    await context.CreateResponseAsync("Updating **profile embed** settings...", true);

    //    var user = await _userRepository.GetByIdAsync(userId); if (user == null) return;

    //    if (betAmount is <= 0 or > 1000) betAmount = DomainConfig.Currency.DefaultBetAmount;

    //    user.BetAmount = betAmount;

    //    context.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
    //        $"{user.Mention}'s currency settings updated{(user.DiscordId != context.User.Id ? $" by {context.User.Mention}" : string.Empty)}."));

    //    await _userRepository.SaveChangesAsync();

    //    context.Interaction.DeleteOriginalResponseAsync(10);

    //}

    [GeneratedRegex("^#(?:[0-9a-fA-F]{3}){1,2}$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex HexCodeRegex();

    [ModalCommand("EmbedSettings")]
    public async Task EmbedSettings(ModalContext context,
        Ulid userId,
        string teamName,
        string profileColor,
        string bio,
        bool displayBoth = false)
    {
        await context.CreateResponseAsync("Updating **profile embed** settings...", true);

        User? user = await userRepository.GetByIdAsync(userId); if (user == null) return;

        user.ProfileColor = HexCodeRegex().IsMatch(profileColor)
            ? profileColor
            : DomainConfig.Profile.DefaultColor;

        user.TeamName = teamName;
        user.Bio = bio;
        user.DisplayBothCharts = displayBoth;

        context.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
            $"{user.Mention}'s profile embed settings have been updated{(user.DiscordId != context.User.Id ? $" by {context.User.Mention}" : string.Empty)}. :ok_hand:"));

        await userRepository.SaveChangesAsync();

        context.Interaction.DeleteOriginalResponseAsync(10);
    }

    [ModalCommand("MatchHistorySettings")]
    public async Task MatchHistorySettings(ModalContext context, Ulid userId, bool displayTournament, bool displayEvent, bool displayCustom)
    {
        await context.CreateResponseAsync("Updating **match history** settings...", true);

        User? user = await userRepository.GetByIdAsync(userId); if (user == null) return;

        user.MatchHistory_DisplayTournament = displayTournament;
        user.MatchHistory_DisplayEvent = displayEvent;
        user.MatchHistory_DisplayCustom = displayCustom;

        context.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
            $"{user.Mention}'s match history settings have been updated{(user.DiscordId != context.User.Id ? $" by {context.User.Mention}" : string.Empty)}."));

        await userRepository.SaveChangesAsync();

        context.Interaction.DeleteOriginalResponseAsync(10);
    }

    [ModalCommand("ChannelAndRoles")] //TODO refactor
    public async Task ChannelAndRoles(ModalContext context, Ulid userId, string channelName, string roleName, string roleSuffix, string roleColor, string? roleIconUrl = null)
    {
        await context.CreateResponseAsync("Updating your __channel__, __roles__ and __emoji__...", true);

        User? user = await userRepository.GetByIdAsync(userId); if (user is null) return;

        if (!user.Vip)
        {
            string logMessage = $"""
                              Resetting __Channel__, __Roles__ and __Emoji__ for {user.Mention} (Reason: Vip value is __false__ and settings triggered by {context.User.Mention})
                              Channel: {user.ChannelMention}
                              Roles: {user.RoleMention} {user.SecondaryRoleMention}
                              > color`{user.RoleColor}`
                              > id`{user.RoleId}` | name`{user.RoleName}`
                              > id`{user.SecondaryRoleId}` | suffix`{user.RoleSuffix}`
                              > iconUrl`{user.RoleIconUrl}`
                              Emoji: id`{user.EmojiId}`{(user.EmojiId is 0 ? string.Empty : $" | {context.Guild.Emojis[user.EmojiId]}")}
                              """;
            discordEngine.Log(logMessage);

            user.RoleColor = string.Empty;
            user.RoleName = string.Empty;
            user.RoleSuffix = string.Empty;
            user.RoleId = default;
            user.SecondaryRoleId = default;
            user.EmojiId = default;
            user.RoleIconUrl = string.Empty;
            user.ChannelId = default;

            await userRepository.SaveChangesAsync();
            context.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"{user.Mention}'s __Channel and Roles settings__ reset by {context.User.Mention}!"));
            return;
        }

        //Roles
        bool validColor = HexCodeRegex().IsMatch(roleColor);
        user.RoleColor = validColor ? roleColor : string.Empty;
        user.RoleName = roleName;
        user.RoleSuffix = roleSuffix;

        if (!validColor)
        {
            context.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Invalid Color.⚠️\nPlease use a __valid hex code__ (#FFFFFF) {context.User.Mention}")
                .AddMention(new UserMention(context.User)));
            return;
        }

        DiscordRole ownerRole;
        if (user.RoleId is 0)
        {
            ownerRole = await context.Guild.CreateRoleAsync(roleName);
            user.RoleId = ownerRole.Id;
        }
        else
        {
            ownerRole = context.Guild.Roles[user.RoleId];
        }

        context.Guild.Members[user.DiscordId].GrantRoleAsync(ownerRole);
        await ownerRole.ModifyAsync(roleName, color: new DiscordColor(roleColor), mentionable: true);

        List<DiscordRole> roles = context.Guild.Roles.OrderBy(r => r.Value.Position)
            .Select(r => r.Value).ToList();
        int position = roles.IndexOf(context.Guild.Roles[DiscordConfig.Roles.SupportersAboveId]) + 1;
        roles.Insert(position, ownerRole);
        Dictionary<int, DiscordRole> reorderedRoles = [];
        int index = 1; roles.ForEach(r => reorderedRoles.Add(index++, r));

        await context.Guild.ModifyRolePositionsAsync(reorderedRoles);

        DiscordRole affiliateRole;
        if (user.SecondaryRoleId is 0)
        {
            affiliateRole = await context.Guild.CreateRoleAsync($"{roleName} - {roleSuffix}");
            user.SecondaryRoleId = affiliateRole.Id;
        }
        else
        {
            affiliateRole = context.Guild.Roles[user.SecondaryRoleId];
        }

        affiliateRole.ModifyAsync($"{roleName} - {roleSuffix}", color: new DiscordColor(roleColor), mentionable: true);

        if (roleIconUrl is not null && roleIconUrl != user.RoleIconUrl)
        {
            try
            {
                await TrySetRoleIconAndEmoji(context, user, roleIconUrl, ownerRole);
            }
            catch (Exception e)
            {
                discordEngine.Log(e);
            }
        }

        //Channel

        if (user.ChannelId is 0)
        {
            DiscordChannel channel = await discordEngine.Guild.CreateChannelAsync(channelName, ChannelType.Voice,
                context.Guild.Channels[DiscordConfig.Channels.SupporterCategoryId]);

            await channel.AddOverwriteAsync(context.Guild.EveryoneRole, deny: Permissions.AccessChannels);
            channel.AddOverwriteAsync(ownerRole, Permissions.AccessChannels);
            channel.AddOverwriteAsync(affiliateRole, Permissions.AccessChannels);

            user.ChannelId = channel.Id;
        }
        else
        {
            context.Guild.Channels[user.ChannelId].ModifyAsync(c => c.Name = channelName);
        }
        user.ChannelName = channelName;

        context.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"{user.Mention}'s __Channel__, __Roles__ and __Emoji__ settings updated!"));

        await userRepository.SaveChangesAsync();

        context.Interaction.DeleteOriginalResponseAsync(10);
    }

    [ModalCommand("ChartSettings")]
    public async Task ChartSettings(ModalContext context, Ulid userId, string mainRating, string secondaryRating, string mainWinrate, string secondaryWinrate, string chartAlpha)
    {
        await context.CreateResponseAsync("Updating **chart** settings...", true);

        User? user = await userRepository.GetByIdAsync(userId); if (user == null) return;

        Regex validColorRegex = ValidHexColor();
        if (validColorRegex.IsMatch(mainRating)) user.Chart_MainRatingColor = mainRating;
        if (validColorRegex.IsMatch(secondaryRating)) user.Chart_SecondaryRatingColor = secondaryRating;
        if (validColorRegex.IsMatch(mainWinrate)) user.Chart_MainWinrateColor = mainWinrate;
        if (validColorRegex.IsMatch(secondaryWinrate)) user.Chart_SecondaryWinrateColor = secondaryWinrate;

        bool validAlpha = double.TryParse(chartAlpha, out double alpha);
        if (validAlpha && alpha is < 0 or > 1) validAlpha = false;
        user.ChartAlpha = validAlpha ? alpha : DomainConfig.Profile.DefaultChartAlpha;

        context.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
            $"{user.Mention}'s chart settings have been updated{(user.DiscordId != context.User.Id ? $" by {context.User.Mention}" : string.Empty)}. :ok_hand:"));

        await userRepository.SaveChangesAsync();

        context.Interaction.DeleteOriginalResponseAsync(10);
    }
    public static (string?, string?, string?) ValidateChampionPool(string meleeInput, string rangedInput, string supportInput, IChampionRepository championRepository)
    {
        string? melee = GetChampionPool(meleeInput, Champion.IsMelee);
        string? ranged = GetChampionPool(rangedInput, Champion.IsRanged);
        string? support = GetChampionPool(supportInput, Champion.IsSupport);

        return (melee, ranged, support);

        string? GetChampionPool(string input, Expression<Func<Champion, bool>> expression)
        {
            input = input.Trim();
            if (input.Equals("All", StringComparison.CurrentCultureIgnoreCase)) return "All";

            char[] separators = ['\n', ',', ';', '/'];
            const string invalidInput = "Invalid input";

            string? championPool = null;
            try
            {
                championPool = input.Split(separators)
                    .Select(m =>
                    {
                        string keyword = m.Trim().ToLower();

                        string name = invalidInput;
                        try
                        {
                            name = championRepository.Get(expression)
                                .SingleOrDefault(c => c.Name.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                                ?.Name ?? invalidInput;
                        }
                        catch { /*ignored*/ }
                        return name;
                    })
                    .Where(s => s != invalidInput)
                    .Aggregate("", (current, name) =>
                    {
                        string line = current.Split('\n').Last();
                        int potentialLength = line.Length + name.Length + 2;

                        return potentialLength <= 15 && !string.IsNullOrWhiteSpace(current)
                            ? $"{current}, {name}"
                            : $"{current}\n{name}";
                    }).Trim();
            }
            catch
            {
                //ignored
            }

            return string.IsNullOrWhiteSpace(championPool) ? null : championPool;
        }
    }

    private async Task TrySetRoleIconAndEmoji(ModalContext context, User user, string roleIconUrl, DiscordRole newRole)
    {
        bool validRoleIcon = true;
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync(new Uri(roleIconUrl));
            response.EnsureSuccessStatusCode();
            await using Stream sourceStream = await response.Content.ReadAsStreamAsync();
            await using MemoryStream iconStream = new();
            await using MemoryStream emojiStream = new();

            double iconSize = (double)sourceStream.Length / 1024; //discord has a max size upload limit of 255KB on these
            double newSize = 0d;
            bool scaled = false;
            if (iconSize >= 255)
            {
                Image image = await Image.LoadAsync(sourceStream);

                image.Mutate(i => i.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Min,
                    Size = new Size(0, 256)
                }));
                scaled = true;

                await image.SaveAsync(iconStream, new PngEncoder());
                await image.SaveAsync(emojiStream, new PngEncoder());
                newSize = (double)iconStream.Length / 1024;
            }
            else
            {
                await sourceStream.CopyToAsync(iconStream);
                sourceStream.Seek(0, SeekOrigin.Begin);
                await sourceStream.CopyToAsync(emojiStream);
            }

            if (DiscordConfig.IsTestBot)
            {
                sourceStream.Seek(0, SeekOrigin.Begin);
                iconStream.Seek(0, SeekOrigin.Begin);

                string format = roleIconUrl.Split(".").Last();

                DiscordMessageBuilder testServerMessage = new DiscordMessageBuilder()
                        .WithContent($"""

                                      **ROLE ICON STUFF**

                                      Can not use the role icon feature on test server but here's some debug info.
                                      Url: `{roleIconUrl}`
                                      Size: `{iconSize:0.## 'KB'}`
                                      Scaled: `{(scaled ? $"true` | `new size: {newSize:0.## 'KB'}" : "false")}`
                                      """)
                        .AddFile($"roleIcon_{newRole.Id}.{format}", sourceStream)
                        .AddFile($"roleIcon_{newRole.Id}_scaled.{format}", iconStream)
                    ;
                await context.Channel.SendMessageAsync(testServerMessage);
            }
            else
            {
                try
                {
                    iconStream.Seek(0, SeekOrigin.Begin);
                    await newRole.ModifyAsync(icon: iconStream);
                }
                catch (Exception e)
                {
                    await discordEngine.Log(e);
                }
            }

            try
            {
                if (user.EmojiId is 0)
                {
                    DiscordGuildEmoji emoji = await discordEngine.Guild.CreateEmojiAsync($"{user.InGameName}_vip", emojiStream);
                    user.EmojiId = emoji.Id;
                }
                else
                {
                    if (user.RoleIconUrl != roleIconUrl)
                    {
                        DiscordGuildEmoji oldEmoji = await discordEngine.Guild.GetEmojiAsync(user.EmojiId);
                        oldEmoji.DeleteAsync("Replaced with new emoji");

                        DiscordGuildEmoji newEmoji = await discordEngine.Guild.CreateEmojiAsync($"{user.InGameName}_vip", emojiStream);
                        user.EmojiId = newEmoji.Id;
                    }
                    else
                    {
                        (await discordEngine.Guild.GetEmojiAsync(user.EmojiId))
                            .ModifyAsync($"{user.InGameName}_vip");
                    }
                }
            }
            catch (Exception e)
            {
                discordEngine.Log(e, $"Setting emoji for {context.User.Mention} threw an exception");
            }

            await sourceStream.DisposeAsync();
            await iconStream.DisposeAsync();
            await emojiStream.DisposeAsync();
        }
        catch (Exception e)
        {
            validRoleIcon = false;
            discordEngine.Log(e, $"Setting role Icon for {context.User.Mention} threw an exception");
        }

        if (validRoleIcon) user.RoleIconUrl = roleIconUrl;
    }

    private async Task SendRegistrationApplication(ModalContext context, User user)
    {
        DSharpPlus.ButtonCommands.ButtonCommandsExtension buttonCommandsExt = context.Client.GetButtonCommands();
        DiscordChannel registrationChannel = discordEngine.Guild.Channels[DiscordConfig.Channels.AdminDashboardId];

        string standard = buttonCommandsExt.BuildButtonId(nameof(AdminButtons.Register), user.Id, AdminButtons.RegistrationAction.Standard);
        string decline = buttonCommandsExt.BuildButtonId(nameof(AdminButtons.Register), user.Id, AdminButtons.RegistrationAction.Decline);

        await registrationChannel.SendMessageAsync(new DiscordMessageBuilder()
            .WithContent($"{context.Member!.Mention} | `{user.InGameName}` | Self evaluation:`{user.Rating_Standard}`")
            .WithAllowedMention(new UserMention(context.Member))
            .AddComponents(
                new DiscordButtonComponent(ButtonStyle.Success, standard, "Approve", false, new DiscordComponentEmoji(DiscordEmoji.FromName(context.Client, ":goat:"))),
                new DiscordButtonComponent(ButtonStyle.Danger, decline, "Decline", false, new DiscordComponentEmoji(DiscordEmoji.FromName(context.Client, ":dog2:")))));
    }

    [GeneratedRegex("^#(?:[0-9a-fA-F]{3}){2}([0-9a-fA-F]{2})?$")]
    internal static partial Regex ValidHexColor();
}
