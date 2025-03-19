using System.Globalization;

using BCL.Discord.Commands.ModalCommands;
using BCL.Domain.Entities.Users;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.ModalCommands;

namespace BCL.Discord.Commands.SlashCommands.Users;
public partial class UserCommands
{
    public static DiscordInteractionResponseBuilder RegistrationModal(string inGameName) =>
        ModalBuilder.Create(nameof(UserModals.Register), inGameName)
            .WithTitle("Register")
            .AddComponents(new TextInputComponent("Self Rating", "selfRating", "Rate yourself as a player from 1 to 10", style: TextInputStyle.Short, max_length: 2))
            .AddComponents(new TextInputComponent("Server", "server", "EU/NA/SA", style: TextInputStyle.Short, max_length: 2))
            .AddComponents(new TextInputComponent("Melee", "melee", "Separate champions with a new line or , ; /", style: TextInputStyle.Paragraph, max_length: 75))
            .AddComponents(new TextInputComponent("Ranged", "ranged", "Jumong, Varesh", style: TextInputStyle.Paragraph, max_length: 75))
            .AddComponents(new TextInputComponent("Support", "support", "Bossom / Jumong", style: TextInputStyle.Paragraph, max_length: 75));
    public static DiscordInteractionResponseBuilder ProfileSettingsModal(User user) =>
        ModalBuilder.Create(nameof(UserModals.ProfileSettings), user.Id.ToString()) //todo overload for this library that uses params object[]
            .WithTitle("Profile Settings")
            .AddComponents(new TextInputComponent("InGameName", "inGameName", "JumongEnjoyer69", user.InGameName, max_length: 30))
            .AddComponents(new TextInputComponent("Server", "server", "EU/NA/SA", user.Server.ToString(), max_length: 2))
            .AddComponents(new TextInputComponent("Melee", "melee", "Separate champions with a new line or , ; /", user.DefaultMelee, style: TextInputStyle.Paragraph, max_length: 75))
            .AddComponents(new TextInputComponent("Ranged", "ranged", "Jumong, Varesh", user.DefaultRanged, style: TextInputStyle.Paragraph, max_length: 75))
            .AddComponents(new TextInputComponent("Support", "support", "Blossom / Jumong", user.DefaultSupport, style: TextInputStyle.Paragraph, max_length: 75));

    public static DiscordInteractionResponseBuilder EmbedSettingsModal(User user) =>
        ModalBuilder.Create(nameof(UserModals.EmbedSettings), user.Id.ToString())
            .WithTitle("Profile Embed Settings")
            .AddComponents(new TextInputComponent("Team name(when you are captain)", "teamName", "Team 3", user.TeamName, required: false, max_length: 23))
            .AddComponents(new TextInputComponent("Profile Color", "ProfileColor", "#FFF123", user.ProfileColor, required: false, max_length: 7))
            .AddComponents(new TextInputComponent("Bio", "bio", "Tell us more about yourself", user.Bio, false, TextInputStyle.Paragraph, max_length: 100))
            .AddComponents(new TextInputComponent("Display both charts", "displayBoth", "Do you want to display both league charts?", user.DisplayBothCharts ? "1" : "0", max_length: 5));

    public static DiscordInteractionResponseBuilder VipChannelModal(User user) =>
        ModalBuilder.Create(nameof(UserModals.ChannelAndRoles), user.Id.ToString())
            .WithTitle("Channel and Roles")
            .AddComponents(new TextInputComponent("Channel Name", "channelName", "No noobs allowed", user.ChannelName, max_length: 20))
            .AddComponents(new TextInputComponent("Role Name", "roleName", "Jumong Onetrick", user.RoleName, max_length: 15))
            .AddComponents(new TextInputComponent("Role Suffix", "roleSuffix", "This is appended to your secondary role (the one you use to give people access to your channel)", user.RoleSuffix, max_length: 15))
            .AddComponents(new TextInputComponent("Role Color", "roleColor", "#FFF123", user.RoleColor, max_length: 7))
            .AddComponents(new TextInputComponent("Role Icon", "roleIconUrl", "https://domain.com/myicon.png", user.RoleIconUrl, required: false, style: TextInputStyle.Paragraph));

    public static DiscordInteractionResponseBuilder MatchHistorySettingsModal(User user) =>
        ModalBuilder.Create(nameof(UserModals.MatchHistorySettings), user.Id.ToString())
            .WithTitle("Match History")
            .AddComponents(new TextInputComponent("Display Tournament Matches ?", "tournament", "0/1", user.MatchHistory_DisplayTournament ? "1" : "0", min_length: 1, max_length: 1))
            .AddComponents(new TextInputComponent("Display Event Matches ?", "event", "0/1", user.MatchHistory_DisplayEvent ? "1" : "0", min_length: 1, max_length: 1))
            .AddComponents(new TextInputComponent("Display Custom Matches ?", "custom", "0/1", user.MatchHistory_DisplayCustom ? "1" : "0", min_length: 1, max_length: 1));

    private const string SupportedHexFormats = "#FFF123 or #FFF123AA";

    public static DiscordInteractionResponseBuilder ChartSettingsModal(User user) =>
        ModalBuilder.Create(nameof(UserModals.ChartSettings), user.Id.ToString())
            .WithTitle("Chart")
            .AddComponents(new TextInputComponent("Main Rating", "mainRating", SupportedHexFormats, user.Chart_MainRatingColor, min_length: 4, max_length: 9))
            .AddComponents(new TextInputComponent("Secondary Rating", "secondaryRating", SupportedHexFormats, user.Chart_SecondaryRatingColor, min_length: 4, max_length: 9))
            .AddComponents(new TextInputComponent("Main Winrate", "mainWinrate", SupportedHexFormats, user.Chart_MainWinrateColor, min_length: 4, max_length: 9))
            .AddComponents(new TextInputComponent("Secondary Winrate", "secondaryWinrate", SupportedHexFormats, user.Chart_SecondaryWinrateColor, min_length: 4, max_length: 9))
            .AddComponents(new TextInputComponent("Chart Alpha", "chartAlpha", "Floating point value between 0 and 1 (example: 0.6)", user.ChartAlpha.ToString(CultureInfo.CurrentCulture), required: false, max_length: 3));

    public static DiscordInteractionResponseBuilder QueueSettingsModal(User user) =>
        ModalBuilder.Create(nameof(UserModals.QueueSettings), user.Id.ToString())
            .WithTitle("Queue")
            .AddComponents(new TextInputComponent("Leave queue after", "purgeAfter", "Time(in minutes) after which to get removed from the queue automatically.", user.PurgeAfter.ToString(), required: false, style: TextInputStyle.Short))
            .AddComponents(new TextInputComponent("Direct Message", "newMatchDm", "Get DM on new match? (0/1)", user.NewMatchDm ? "1" : "0", required: true, style: TextInputStyle.Short, min_length: 1, max_length: 1));

    //public static DiscordInteractionResponseBuilder CurrencySettings(User user) =>
    //    ModalBuilder.Create(nameof(UserModals.CurrencySettings), user.Id.ToString())
    //        .WithTitle("Currency")
    //        .AddComponents(new TextInputComponent("Bet amount", "betAmount", "Custom Bet amount (max 1000)", user.BetAmount.ToString(CultureInfo.InvariantCulture), max_length: 4, required: false));
}
