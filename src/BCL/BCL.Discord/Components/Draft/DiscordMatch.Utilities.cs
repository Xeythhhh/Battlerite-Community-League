using System.Diagnostics;

using BCL.Domain.Entities.Matches;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;

using DSharpPlus;
using DSharpPlus.Entities;

#pragma warning disable CS4014

namespace BCL.Discord.Components.Draft;
public partial class DiscordMatch
{
    public void MakeCaptain(DiscordUser newCaptain) => MakeCaptain(newCaptain.Id);
    public void MakeCaptain(ulong newCaptainId)
    {
        if (DraftFinishedAt is not null && Match.Draft.IsFinished) return;

        Match.Side side = Match.GetSide(newCaptainId);

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (side)
        {
            case Match.Side.Team1:
                Match.Draft.Captain1DiscordId = newCaptainId;
                Team1.MakeCaptain(newCaptainId);

                if (Team1.ChannelRenamedCount < 2) // avoid ratelimit
                {
                    Team1.TextChannel.ModifyAsync(channelEditModel => channelEditModel.Name = Team1.Name);
                    Team1.VoiceChannel?.ModifyAsync(channelEditModel => channelEditModel.Name = Team1.Name);
                    Team1.ChannelRenamedCount++;
                }

                if (Match.Draft.IsTeam1Turn || Match.Draft.DraftType is DraftType.Simultaneous) DraftActionStopwatch.Restart();

                break;

            case Match.Side.Team2:
                Match.Draft.Captain2DiscordId = newCaptainId;
                Team2.MakeCaptain(newCaptainId);

                if (Team2.ChannelRenamedCount < 2) // avoid ratelimit
                {
                    Team2.TextChannel.ModifyAsync(channelEditModel => channelEditModel.Name = Team2.Name);
                    Team2.VoiceChannel?.ModifyAsync(channelEditModel => channelEditModel.Name = Team2.Name);
                    Team2.ChannelRenamedCount++;
                }

                if (!Match.Draft.IsTeam1Turn || Match.Draft.DraftType is DraftType.Simultaneous) DraftActionStopwatch.Restart();

                break;

            default: throw new UnreachableException();
        }

        if (Ready && CurrentStep is null) Start();
        else Update(false);
    }

    public record SubPayload(User User, DiscordUser DiscordUser);
    public async Task Sub(SubPayload afk, SubPayload sub)
    {
        await Gate.WaitAsync(5000);

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (Match.GetSide(afk.User))
        {
            case Match.Side.Team1:
                Match.Team1 = Match.Team1.Replace(afk.User.Id.ToString(), sub.User.Id.ToString());
                Match.Team1 = Match.Team1.Replace(afk.DiscordUser.Id.ToString(), sub.DiscordUser.Id.ToString());
                Team1.Sub(afk.User, sub.User);

                Team1.TextChannel.AddOverwriteAsync((DiscordMember)sub.DiscordUser, Permissions.AccessChannels);
                Team1.VoiceChannel?.AddOverwriteAsync((DiscordMember)sub.DiscordUser, Permissions.AccessChannels);

                Team2.TextChannel.AddOverwriteAsync((DiscordMember)sub.DiscordUser, deny: Permissions.AccessChannels);
                Team2.VoiceChannel?.AddOverwriteAsync((DiscordMember)sub.DiscordUser, deny: Permissions.AccessChannels);

                Team1.TextChannel.AddOverwriteAsync((DiscordMember)afk.DiscordUser, deny: Permissions.AccessChannels);
                Team1.VoiceChannel?.AddOverwriteAsync((DiscordMember)afk.DiscordUser, deny: Permissions.AccessChannels);

                break;

            case Match.Side.Team2:
                Match.Team2 = Match.Team2.Replace(afk.User.Id.ToString(), sub.User.Id.ToString());
                Match.Team2 = Match.Team2.Replace(afk.DiscordUser.Id.ToString(), sub.DiscordUser.Id.ToString());
                Team2.Sub(afk.User, sub.User);

                Team2.TextChannel.AddOverwriteAsync((DiscordMember)sub.DiscordUser, Permissions.AccessChannels);
                Team2.VoiceChannel?.AddOverwriteAsync((DiscordMember)sub.DiscordUser, Permissions.AccessChannels);

                Team1.TextChannel.AddOverwriteAsync((DiscordMember)sub.DiscordUser, deny: Permissions.AccessChannels);
                Team1.VoiceChannel?.AddOverwriteAsync((DiscordMember)sub.DiscordUser, deny: Permissions.AccessChannels);

                Team2.TextChannel.AddOverwriteAsync((DiscordMember)afk.DiscordUser, deny: Permissions.AccessChannels);
                Team2.VoiceChannel?.AddOverwriteAsync((DiscordMember)afk.DiscordUser, deny: Permissions.AccessChannels);
                break;

            default: throw new UnreachableException();
        }

        Match._discordUserIds = Match._discordUserIds.Replace(afk.DiscordUser.Id.ToString(), sub.DiscordUser.Id.ToString());
        _reports.RemoveAll(r => r.DiscordId == afk.DiscordUser.Id);
        ReadyCheckEntries.Remove(afk.DiscordUser.Id);

        if (Match.IsCaptain(afk.DiscordUser)) MakeCaptain(sub.DiscordUser);
        else Update();

        Gate.Release();
    }
}
