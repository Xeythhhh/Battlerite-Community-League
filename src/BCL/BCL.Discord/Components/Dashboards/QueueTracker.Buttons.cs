using BCL.Discord.Commands.ButtonCommands;
using BCL.Domain.Enums;
using DSharpPlus.Entities;
using DSharpPlus;

namespace BCL.Discord.Components.Dashboards;
public partial class QueueTracker
{
    DiscordComponent[] QueueButtons => [
            new DiscordButtonComponent(
                ButtonStyle.Primary,
                _buttonCommandsExtension.BuildButtonId(nameof(QueueTrackerButtons.JoinQueue), QueueRole.Fill),
                "Solo",
                false,
                new DiscordComponentEmoji(DiscordEmoji.FromName(_client, ":heavy_check_mark:"))),

            new DiscordButtonComponent(
                ButtonStyle.Primary,
                _buttonCommandsExtension.BuildButtonId(nameof(QueueTrackerButtons.JoinTeamQueue)),
                "Team",
                false,
                new DiscordComponentEmoji(DiscordEmoji.FromName(_client, ":heavy_check_mark:"))),

            new DiscordButtonComponent(
                ButtonStyle.Danger,
                _buttonCommandsExtension.BuildButtonId(nameof(QueueTrackerButtons.LeaveQueue)),
                "Leave",
                false,
                new DiscordComponentEmoji(DiscordEmoji.FromName(_client, ":heavy_multiplication_x:"))),

            new DiscordButtonComponent(
                ButtonStyle.Success,
                _buttonCommandsExtension.BuildButtonId(nameof(QueueTrackerButtons.TestRefresh)),
                "Refresh",
                false,
                new DiscordComponentEmoji(DiscordEmoji.FromName(_client, ":test_tube:")))];

    //FAKE
    DiscordComponent[] QueueSettingsButtons => [
            new DiscordButtonComponent(
                ButtonStyle.Secondary,
                _buttonCommandsExtension.BuildButtonId(nameof(UserSettingsButtons.ToggleQueueSetting),
                    UserSettingsButtons.QueueSetting.Standard),
                "Standard",
                false),

            new DiscordButtonComponent(
                ButtonStyle.Secondary,
                _buttonCommandsExtension.BuildButtonId(nameof(UserSettingsButtons.ToggleQueueSetting),
                    UserSettingsButtons.QueueSetting.Pro),
                "Pro",
                false),

            new DiscordButtonComponent(
                ButtonStyle.Secondary,
                _buttonCommandsExtension.BuildButtonId(nameof(UserSettingsButtons.ToggleQueueSetting),
                    UserSettingsButtons.QueueSetting.Eu),
                "EU",
                false),

            new DiscordButtonComponent(
                ButtonStyle.Secondary,
                _buttonCommandsExtension.BuildButtonId(nameof(UserSettingsButtons.ToggleQueueSetting),
                    UserSettingsButtons.QueueSetting.Na),
                "NA",
                false),

            new DiscordButtonComponent(
            ButtonStyle.Secondary,
            _buttonCommandsExtension.BuildButtonId(nameof(UserSettingsButtons.ToggleQueueSetting),
                UserSettingsButtons.QueueSetting.Sa),
                "SA",
                false)
        ];

    private DiscordComponent[] TestQueueButtons => [
            new DiscordButtonComponent(
                ButtonStyle.Success,
                _buttonCommandsExtension.BuildButtonId(nameof(QueueTrackerButtons.TestQueue)),
                "Test Queue Pop",
                false,
                new DiscordComponentEmoji(DiscordEmoji.FromName(_client, ":test_tube:")))
        ];

    //REAL
    //DiscordComponent[] QueueSettingsButtons =>
    //    new DiscordComponent[] {

    //        new DiscordButtonComponent(
    //            ButtonStyle.Secondary,
    //            _buttonCommandsExtension.BuildButtonId(nameof(UserSettingsButtons.ToggleQueueSetting),
    //                UserSettingsButtons.QueueSetting.Standard),
    //            "Standard",
    //            false,
    //            new DiscordComponentEmoji(DiscordEmoji.FromName(_client, ":heh:"))),

    //        new DiscordButtonComponent(
    //            ButtonStyle.Secondary,
    //            _buttonCommandsExtension.BuildButtonId(nameof(UserSettingsButtons.ToggleQueueSetting),
    //                UserSettingsButtons.QueueSetting.Pro),
    //            "Pro",
    //            false,
    //            new DiscordComponentEmoji(DiscordEmoji.FromName(_client, ":goat:"))),

    //        new DiscordButtonComponent(
    //            ButtonStyle.Secondary,
    //            _buttonCommandsExtension.BuildButtonId(nameof(UserSettingsButtons.ToggleQueueSetting),
    //                UserSettingsButtons.QueueSetting.Eu),
    //            "EU",
    //            false,
    //            new DiscordComponentEmoji(DiscordEmoji.FromName(_client, ":EU:"))),

    //        new DiscordButtonComponent(
    //            ButtonStyle.Secondary,
    //            _buttonCommandsExtension.BuildButtonId(nameof(UserSettingsButtons.ToggleQueueSetting),
    //                UserSettingsButtons.QueueSetting.Na),
    //            "NA",
    //            false,
    //            new DiscordComponentEmoji(DiscordEmoji.FromName(_client, ":NA:"))),

    //        new DiscordButtonComponent(
    //        ButtonStyle.Secondary,
    //        _buttonCommandsExtension.BuildButtonId(nameof(UserSettingsButtons.ToggleQueueSetting),
    //            UserSettingsButtons.QueueSetting.Sa),
    //            "SA",
    //            false,
    //            new DiscordComponentEmoji(DiscordEmoji.FromName(_client, ":SA:"))
    //            )
    //    };
}
