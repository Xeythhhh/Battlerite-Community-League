using System.Diagnostics.CodeAnalysis;

using BCL.Discord.Components.Dashboards;
using BCL.Discord.Extensions;
using BCL.Discord.OptionProviders;
using BCL.Domain;
using BCL.Domain.Entities.Analytics;
using BCL.Domain.Entities.Queue;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus;
using DSharpPlus.ButtonCommands;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

using Microsoft.Extensions.DependencyInjection;

namespace BCL.Discord.Commands.SlashCommands.Users;

[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
public partial class UserCommands
{
    [SlashCommandGroup("UserUtils", "Bunch of random stuff.")]
    [SlashModuleLifespan(SlashModuleLifespan.Scoped)]
    public class UserUtils(IUserRepository userRepository, IAnalyticsRepository analytics) : ApplicationCommandModule
    {
        //[SlashCommand("ApplyForPro", "Apply for pro league")]
        //public async Task ApplyForPro(InteractionContext context)
        //{
        //    await context.DeferAsync();

        //    var user = _userRepository.GetByDiscordUser(context.User) ?? throw new Exception("User is not registered");
        //    if (user.Pro) throw new Exception("User already has pro membership");
        //    if (_discordEngine.ProLeagueManager.Applications.TryGetValue(context.User.Id, out var application) && application.Message is not null)
        //        throw new Exception("User application under review!");
        //    if (user.ProApplicationTimeout > DateTime.UtcNow)
        //        throw new Exception($"User is timed out from applying to pro league until {user.ProApplicationTimeout}");

        //    _discordEngine.ProLeagueManager.Applications.Remove(context.User.Id);

        //    var content = $"""
        //                   {user.Mention}
        //                   Region: **{user.Server}**
        //                   Last Played: **{user.LastPlayed?.DiscordTime() ?? "Never"}**
        //                   ```ml
        //                   {user.InGameName}
        //                   {user.SeasonStats.OrderByDescending(s => s.RecordedAt).Aggregate("", (s, stats) => $"{s}\n    {stats.Season.FormatForDiscordCode(15)} | {(stats.PlayedPro ? stats.LatestSnapshot?.Rating : stats.LatestSnapshot?.Rating_Standard)?.ToString().FormatForDiscordCode(4, true) ?? "----"} | {((stats.PlayedPro ? stats.LatestSnapshot?.WinRate_Pro : stats.LatestSnapshot?.WinRate_Standard) ?? 0).ToString("0.00%").FormatForDiscordCode(7)} | {((stats.PlayedPro ? stats.LatestSnapshot?.GamesPlayed_Pro : stats.LatestSnapshot?.GamesPlayed_Standard) ?? 0).FormatForDiscordCode(3, true)} Matches | {(stats.PlayedPro ? "Pro" : "Standard")}")}
        //                   ```
        //                   """;

        //    var regionRoleId = user.Server switch
        //    {
        //        Region.Eu => DiscordConfig.Roles.Region.EuId,
        //        Region.Na => DiscordConfig.Roles.Region.NaId,
        //        Region.Sa => DiscordConfig.Roles.Region.SaId,

        //        Region.Unknown => throw new NotImplementedException(),
        //        _ => throw new UnreachableException()
        //    };

        //    await context.Guild.Members[user.DiscordId].GrantRoleAsync(context.Guild.Roles[regionRoleId]);

        //    var channelId = user.Server switch
        //    {
        //        Region.Eu => context.Client.GetChannelAsync(DiscordConfig.Channels.Pro.EuId),
        //        Region.Na => context.Client.GetChannelAsync(DiscordConfig.Channels.Pro.NaId),
        //        Region.Sa => context.Client.GetChannelAsync(DiscordConfig.Channels.Pro.SaId),

        //        Region.Unknown => throw new NotImplementedException(),
        //        _ => throw new UnreachableException()
        //    };

        //    var message = await new DiscordMessageBuilder()
        //        .WithContent($"<@&{regionRoleId}> New Pro League application, please review! If you are unsure, discuss it with your peers.")
        //        .AddEmbed(new DiscordEmbedBuilder()
        //            .WithDescription(content)
        //            .AddField("Reviewed by", $"0 / {_userRepository.GetAll().Count(u => u.Server == user.Server && u.Pro)} Players"))
        //        .AddMention(new RoleMention(regionRoleId))
        //        .AddComponents(ProLeagueButtons.ApplicationButtons(user.DiscordId, _discordEngine))
        //        .SendAsync(await channelId);

        //    await _discordEngine.ProLeagueManager.AddApplication(message, context.User.Id, user.Server);

        //    await context.EditResponseAsync(new DiscordWebhookBuilder()
        //        .WithContent("Application submitted"));
        //}

        [SlashCommand("Suggest", "Suggest a user to use a command")]
        public async Task Suggest(InteractionContext context,
            [Option("User", "Discord User")] DiscordUser discordUser,
            [Autocomplete(typeof(CommandAutocompleteProvider))]
            [Option("Suggestion", "Command name")] string commandMention) //lol can't have ulong as command option unlucky
        {
            const string content = "nice :ok_hand:";

            DiscordMessageBuilder message = new DiscordMessageBuilder()
                .WithContent($"{discordUser.Mention} try using {commandMention}!")
                .WithAllowedMention(new UserMention(discordUser.Id));

            await message.SendAsync(context.Channel);
            await context.CreateResponseAsync(content);
            await context.DeleteResponseAsync(1);
        }

        //[SlashCommand_Supporter]
        //[SlashCommand("InviteToPrivateChannel", "Give a user access to your private channel!", false)]
        //public async Task InviteToPrivateChannel(InteractionContext context,
        //    [Option("User", "Discord user")] DiscordUser discordUser,
        //    [Option("Value", "True or False")] bool value = true)
        //{
        //    var user = _userRepository.GetByDiscordId(context.User.Id);
        //    if (user is null) { await SuggestRegistration(context); return; }
        //    if (!user.Vip) { await context.CreateResponseAsync("Only available to bcl supporters.", true); return; }
        //    if (user.ChannelId is 0) { await context.CreateResponseAsync("Set up your channel first.", true); return; }

        //    var discordMember = _discordEngine.Guild.Members[discordUser.Id];
        //    switch (value)
        //    {
        //        case true:
        //            await discordMember.GrantRoleAsync(_discordEngine.Guild.Roles[user.SecondaryRoleId]);
        //            break;

        //        case false:
        //            await discordMember.RevokeRoleAsync(_discordEngine.Guild.Roles[user.SecondaryRoleId]);
        //            break;
        //    }

        //    var content = $"Access to {_discordEngine.Guild.Channels[user.ChannelId].Mention} set to `{value}` for {discordUser.Mention} by {context.User.Mention}";

        //    await context.CreateResponseAsync(content);
        //    await _discordEngine.Log(content);
        //}

        public enum ProfileStyling
        {
            //TODO ProfileStyling0: script to pull all of these from highlight.js and paste a
            //TODO ProfileStyling1: template codeblock message to expand this enum
            //TODO ProfileStyling2: extract an example to post as docs so people dont have to look tru all of them

            // ReSharper disable InconsistentNaming
            ldif, js, cs, diff, ruby, make, swift, yaml, ts,
            vb, m, ml, st, elixir, ahk, brainfuck, crystal, fix,
            haxe, monkey
            // ReSharper restore InconsistentNaming
        }

        //[SlashCommand_Supporter]
        //[SlashCommand("StylingShowcase", "Sends a dm with styles you can use")]
        //public async Task StylingShowcase(InteractionContext context)
        //{
        //    var dmChannel = await context.Member.CreateDmChannelAsync();
        //    if (dmChannel == null)
        //    {
        //        await context.CreateResponseAsync("Enable bot dm's to use this feature");
        //        return;
        //    }

        //    var content = string.Empty;
        //    var index = 0;
        //    foreach (ProfileStyling style in Enum.GetValues(typeof(ProfileStyling)))
        //    {
        //        index++;
        //        content += $"""

        //                    **{style}**
        //                    Champion Pool:
        //                    ```{style}
        //                    Jumong, Varesh
        //                    Taya, Jade
        //                    Shen Rao
        //                    ```
        //                    Elo:
        //                    ```{style}
        //                    50.00%           420
        //                    MMR              696
        //                    ⮙1234          ⮛690
        //                    Streak:           +4
        //                    ```
        //                    ====================
        //                    """;

        //        if (index != 4) continue;

        //        await dmChannel.SendMessageAsync(content);
        //        content = string.Empty;
        //        index = 0;
        //    }

        //    await dmChannel.SendMessageAsync(content);
        //}

        #region Leaderboard

        [SlashCommand("Leaderboard", "Displays the leaderboard", false)]
        public async Task Leaderboard(InteractionContext context)
        {
            string defaultSeason = DomainConfig.Season;
            await context.CreateResponseAsync($"Generating leaderboard for `{defaultSeason}`...");
            User? user = userRepository.GetByDiscordUser(context.User);

            string[] defaultFilters = [$"{((user?.Pro ?? true) ? League.Pro : League.Standard)}|League", $"{user?.Server ?? Region.Eu}|Region", $"{DomainConfig.Season}|Season"];
            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(GetLeaderboard(
                    Stats.GetGenericEmbed(context.Guild.IconUrl),
                    user,
                    defaultFilters,
                    context.Services))
                .AddComponents(BuildFilterSelectComponent(
                    null, context.User.Id, context.Guild.Emojis,
                    defaultFilters.Select(s => s.Split("|").First()).ToArray(),
                    SelectOption.GetOptions(analytics.GetMigrationInfo()!, true,
                        QueueTracker.MatchesField.QueueInfos.Where(q =>
                        q.FilteredBy is QueueTracker.MatchesField.Filter.FilterType.League
                                     or QueueTracker.MatchesField.Filter.FilterType.Region)),
                    SelectFor.Leaderboard)));

            //embed.AddField("Disclaimer", "*You need to have at least **25** games played in the league to qualify.*");

            //await context.EditResponseAsync(new DiscordWebhookBuilder()
            //    .AddEmbed(embed));
        }

        public record LeaderboardFilter(string Value, QueueTracker.MatchesField.Filter.FilterType FilterType);
        public record LeaderboardEntry(string Mention, string InGameName, double Value);
        internal static DiscordEmbed GetLeaderboard(
            DiscordEmbedBuilder embed,
            User? requestOwner,
            string[] filters,
            IServiceProvider services)
        {
            List<LeaderboardEntry> leaderBoardEntries = [];
            string? league = null;
            string? warnings = null;

            List<IGrouping<QueueTracker.MatchesField.Filter.FilterType, LeaderboardFilter>> filterGroups = filters.Select(f =>
                {
                    string[] values = f.Split("|");
                    return new LeaderboardFilter(values[0],
                        Enum.Parse<QueueTracker.MatchesField.Filter.FilterType>(values[1]));
                })
                .GroupBy(g => g.FilterType)
                .ToList();

            List<LeaderboardFilter>? regionFilters = filterGroups.FirstOrDefault(g => g.Key is QueueTracker.MatchesField.Filter.FilterType.Region)?.ToList();
            List<LeaderboardFilter>? leagueFilters = filterGroups.FirstOrDefault(g => g.Key is QueueTracker.MatchesField.Filter.FilterType.League)?.ToList();
            List<LeaderboardFilter>? seasonFilters = filterGroups.FirstOrDefault(g => g.Key is QueueTracker.MatchesField.Filter.FilterType.Season)?.ToList();

            using IServiceScope scope = services.CreateScope();
            MigrationInfo migrationInfo = scope.ServiceProvider.GetRequiredService<IAnalyticsRepository>().GetMigrationInfo()!;
            string seasonData;

            if (seasonFilters?.Count != 0 ||
                seasonFilters.Count == migrationInfo.Seasons.Length)
            {
                seasonData = " `All`";
                if (seasonFilters?.Count == 0)
                {
                    seasonFilters = migrationInfo.Seasons
                        .Select(s =>
                            new LeaderboardFilter(s.Label, QueueTracker.MatchesField.Filter.FilterType.Season))
                        .ToList();
                }
            }
            else
            {
                seasonData = seasonFilters.Aggregate("", (current, next) => $"{current} `{next.Value}`");
            }

            IEnumerable<User> users = scope.ServiceProvider.GetRequiredService<IUserRepository>().GetAll();
            bool isActivity = filterGroups.Any(f => f.Key is QueueTracker.MatchesField.Filter.FilterType.Activity);
            if (!isActivity)
            {
                if (leagueFilters?.Count > 1)
                    warnings += "\n-Ranking leaderboards don't support multiple leagues!";

                if (leagueFilters?.Any(f => f.Value != "Pro" && f.Value != "Standard") ?? false)
                    warnings += "\n-Ranking leaderboards only support \"Pro\" and \"Standard\"!";

                league = leagueFilters?.LastOrDefault(f => f.Value is "Pro" or "Standard")?.Value
                    ?? requestOwner?.Server.ToString()
                    ?? "Pro";

                //Make sure the changes are reflected on the embed
                leagueFilters = [new(league, QueueTracker.MatchesField.Filter.FilterType.League)];
            }

            string leagueData = leagueFilters is null || leagueFilters.Count == Enum.GetValues<League>().Length
                ? " `All`"
                : leagueFilters.Aggregate("", (current, next) => $"{current} `{next.Value}`");

            string regionData = regionFilters is null || regionFilters.Count == Enum.GetValues<Region>().Length
                ? " `All`"
                : regionFilters.Aggregate("", (current, next) => $"{current} `{next.Value}`");

            foreach (User user in users)
            {
                if (!user.Approved || (!(regionFilters?.Any(r => r.Value.Equals(user.Server.ToString(), StringComparison.CurrentCultureIgnoreCase)) ?? true))) continue;

                IEnumerable<Domain.Entities.Users.Stats> stats = user.SeasonStats.Where(s =>
                    (seasonFilters ?? [new LeaderboardFilter(DomainConfig.Season, QueueTracker.MatchesField.Filter.FilterType.Season)])
                        .Any(f =>
                            f.Value.Equals(s.Season, StringComparison.CurrentCultureIgnoreCase)));

                if (!isActivity && league is "Standard")
                    stats = stats.Where(s => s is { Membership: League.Standard, PlayedPro: false });

                double value = isActivity

                    ? stats.Sum(s =>
                    {
                        int played = 0;
                        if (leagueFilters is not null)
                        {
                            played = CalculatePlayed(s, leagueFilters, played);
                        }
                        else
                        {
                            played += s.LatestSnapshot?.GamesPlayed ?? 0;
                        }

                        return played;
                    })

                    : stats.Max(s => league is "Pro"

                        ? s.LatestSnapshot?.GamesPlayed_Pro < 25
                            ? 0
                            : (s.LatestSnapshot?.Rating)

                        : s.LatestSnapshot?.GamesPlayed_Standard < 25
                            ? 0
                            : (s.LatestSnapshot?.Rating_Standard)) ?? 0;

                leaderBoardEntries.Add(new LeaderboardEntry(user.Mention, user.InGameName, value));
            }

            leaderBoardEntries = leaderBoardEntries
                .OrderByDescending(e => e.Value)
                .Take(20)
                .Where(e => e.Value != 0)
                .ToList();

            embed.WithDescription($"""
                                   __Selection__:
                                   Seasons:{seasonData}
                                   Leagues:{leagueData}
                                   Regions:{regionData}
                                   Type: `{(isActivity ? "Activity" : "Ranking")}`

                                   *`Rank {(isActivity ? "Matches" : "Rating")} Discord InGameName`*
                                   """)
                .AddField("Leaderboard", GetLeaderboardContent(leaderBoardEntries));

            if (warnings is not null) embed.AddField("Warnings", $"```diff\n{warnings}\n```");
            return embed;
        }

        private static int CalculatePlayed(Domain.Entities.Users.Stats s, List<LeaderboardFilter> leagueFilters, int played)
        {
            played += leagueFilters
                .Select((LeaderboardFilter leagueFilter) => s.LatestSnapshot?.GetType().GetProperty($"GamesPlayed_{leagueFilter.Value}"))
                .Sum((gamesPlayedLeagueProperty) => (int)(gamesPlayedLeagueProperty?.GetValue(s.LatestSnapshot) ?? 0));
            return played;
        }

        private static string GetLeaderboardContent(List<LeaderboardEntry> leaderboardEntries)
        {
            if (leaderboardEntries.Count == 0) return "No eligible players";

            int index = 1;
            LeaderboardEntry first = leaderboardEntries[0];
            leaderboardEntries.Remove(first);

            return leaderboardEntries.Aggregate($":crown: :crown: __**{first.Value}**__ {first.Mention} `{first.InGameName}` :crown: :crown:",
                (current, user) => $"""
                                    {current}
                                    `#{(++index).FormatForDiscordCode(2)} | {(user.Value).FormatForDiscordCode(4, true)}` {user.Mention} `{user.InGameName}`
                                    """)
                                .Trim();
        }

        #endregion

        //[SlashCommand("SetStyling", "Set your profile style!")]
        //public async Task SetStyling(InteractionContext context,
        //    [Option("Style", "Style option")] ProfileStyling style = ProfileStyling.ml)
        //{
        //    await context.DeferAsync();

        //    var user = _userRepository.GetByDiscordUser(context.User)!;
        //    if (!user.Vip)
        //    {
        //        //Didn't use [SlashCommand_Supporter] attribue so it can be used in dms,
        //        //this attribute requires context.Member which is only present in a Guild
        //        await context.FollowUpAsync(new DiscordFollowupMessageBuilder()
        //            .WithContent("Only available to bcl supporters! :sunglasses:"));
        //        return;
        //    }
        //    user.Styling = style.ToString();
        //    _userRepository.SaveChanges();

        //    await context.FollowUpAsync(
        //        new DiscordFollowupMessageBuilder()
        //            .WithContent($"{user.Mention}'s style set to `{style}`"));

        //    await _SendProfileMessage(context, context.User, user, new[] { DomainConfig.Season }, _discordEngine);
        //}
    }

    public static async Task SuggestRegistration(BaseContext context) =>
        await SuggestRegistration(context.Interaction, context.Client, context.User);
    public static async Task SuggestRegistration(BaseContext context, DiscordUser discordUser) =>
        await SuggestRegistration(context.Interaction, context.Client, discordUser);
    public static async Task SuggestRegistration(ButtonContext context, bool editOriginal = false) =>
        await SuggestRegistration(context.Interaction, context.Client, context.User, editOriginal);

    public static async Task SuggestRegistration(
        DiscordInteraction interaction,
        DiscordClient client,
        DiscordUser discordUser,
        bool editOriginal = false,
        string? message = null)
    {
        string content = $"{message ?? $"{discordUser.Mention} is not registered."} | Try {client.MentionCommand<UserCommands>(nameof(Register))}!";

        switch (editOriginal)
        {
            case true:
                await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent(content));
                break;
            case false:
                await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .WithContent(content));
                break;
        }
    }

    public enum SelectFor
    {
        Profile,
        //Wallet,
        RegionStats,
        ChampionStats,
        ChampionProfile,
        Leaderboard
    }
    public class SelectOption(
        string value,
        string description,
        DiscordComponentEmoji? emoji = null,
        QueueTracker.MatchesField.Filter.FilterType filterType = QueueTracker.MatchesField.Filter.FilterType.Season)
    {
        public string Label { get; set; } = value;
        public string Description { get; set; } = description;
        public DiscordComponentEmoji? Emoji { get; set; } = emoji;
        public QueueTracker.MatchesField.Filter.FilterType FilterTypeType { get; set; } = filterType;

        public static IEnumerable<SelectOption> GetOptions(User user)
        {
            return user.SeasonStats.Where(s => s.LatestSnapshot?.GamesPlayed > 0)
                .OrderBy(s => s.RecordedAt).Select(s =>
            {
                string rating = $"Played: {s.LatestSnapshot?.GamesPlayed ?? 0} | Rating: ";
                if (user.Pro)
                {
                    rating += $"{s.LatestSnapshot?.Rating ?? user.Rating}";
                    if (user.Vip) rating += $" / {s.LatestSnapshot?.Rating_Standard ?? user.Rating_Standard}";
                }
                else
                {
                    rating += $"{s.LatestSnapshot?.Rating_Standard ?? user.Rating_Standard}";
                }

                return new SelectOption(s.Season,
                                rating);
            });
        }

        public static IEnumerable<SelectOption> GetOptions(MigrationInfo migrationInfo, bool displayMatches = false, IEnumerable<QueueTracker.MatchesField.Filter>? queueInfos = null)
        {
            List<SelectOption> selectOptions = migrationInfo.Seasons.Select(s =>
                {
                    string description = $"Started: {s.StartDate:yyyy MMMM dd}";
                    if (displayMatches) description += $" | Matches: {s.MatchCount}";
                    return new SelectOption(s.Label, description);
                }).ToList();

            if (queueInfos is null) return selectOptions;

            foreach (QueueTracker.MatchesField.Filter? queueInfo in queueInfos.Where(q =>
                q.FilteredBy is QueueTracker.MatchesField.Filter.FilterType.Region
                             or QueueTracker.MatchesField.Filter.FilterType.League))
            {
                selectOptions.Add(new SelectOption(
                    queueInfo.Key,
                    $" Matches: {queueInfo.Total}",
                    null,
                    queueInfo.FilteredBy));
            }

            selectOptions.Add(new SelectOption("Activity", string.Empty, null, QueueTracker.MatchesField.Filter.FilterType.Activity));

            return selectOptions;
        }

        public static IEnumerable<SelectOption> GetOptions(Champion champion) =>
            champion.Stats.OrderBy(s => s.RecordedAt).Select(s => new SelectOption(s.Season,
                $"Matches: {s.LatestSnapshot?.GamesPlayed ?? 0}"));
    }

    /// <summary>
    /// Builds the Filter for embeds
    /// <para>For RegionStats provide options and null for User</para>
    /// </summary>
    /// <param name="id">User or Champion Id</param>
    /// <param name="componentOwner">Who can edit the component</param>
    /// <param name="emojis">Emoji collection to search season icons in</param>
    /// <param name="selected">Previous selection</param>
    /// <param name="options">When you provide a user, the options are built conditionally, use this if you don't provide a User</param>
    /// <param name="componentType">Embed type to build for</param>
    /// <returns>preconfigured DiscordSelectComponent</returns>
    public static DiscordSelectComponent BuildFilterSelectComponent(
        Ulid? id, //do i need this?????????????
        ulong componentOwner,
        IReadOnlyDictionary<ulong, DiscordEmoji> emojis,
        string[] selected,
        IEnumerable<SelectOption> options,
        SelectFor componentType = SelectFor.Profile)
    {
        List<SelectOption> selectOptions = options.ToList();
        if (selectOptions.Count > 25) throw new ArgumentException("Too many options");

        List<DiscordSelectComponentOption> componentOptions = selectOptions.ConvertAll(s =>
            {
                if (s.Emoji is null)
                {
                    DiscordEmoji? emoji = s.FilterTypeType switch
                    {
                        QueueTracker.MatchesField.Filter.FilterType.Activity or
                        QueueTracker.MatchesField.Filter.FilterType.Region or
                        QueueTracker.MatchesField.Filter.FilterType.League => emojis.Values.FirstOrDefault(e =>
                            e.Name.Equals(s.Label, StringComparison.CurrentCultureIgnoreCase)),

                        QueueTracker.MatchesField.Filter.FilterType.Season => emojis.Values.FirstOrDefault(e =>
                            e.Name.Contains($"{DomainConfig.ServerAlias}_{s.Label.Replace(' ', '_')}", StringComparison.CurrentCultureIgnoreCase)),

                        QueueTracker.MatchesField.Filter.FilterType.All => throw new NotImplementedException(),
                        _ => throw new NotImplementedException(),
                    };

                    s.Emoji = emoji is null ? null : new DiscordComponentEmoji(emoji);
                }

                string value = componentType switch
                {
                    SelectFor.Leaderboard => $"{s.Label}|{s.FilterTypeType}",
                    _ => s.Label
                };

                return new DiscordSelectComponentOption(s.Label, value, s.Description,
                    selected.Contains(value), s.Emoji);
            });

        string placeholder = componentType switch
        {
            //SelectFor.Wallet => "This affects chart and last transaction.",
            SelectFor.Leaderboard => "Filters",
            _ => "Select one or more seasons."
        };

        return new DiscordSelectComponent($"SeasonSelect|{componentType}|{id ?? Ulid.Empty}|{componentOwner}", placeholder,
            componentOptions, false, 1, componentOptions.Count);
    }
}
