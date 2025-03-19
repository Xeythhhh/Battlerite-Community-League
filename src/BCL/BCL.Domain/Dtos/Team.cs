using BCL.Domain.Entities.Abstract;
using BCL.Domain.Entities.Matches;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;

using DSharpPlus;
using DSharpPlus.Entities;

#pragma warning disable CS8618

namespace BCL.Domain.Dtos;
public class Team
{
    public Team(List<Player> players, bool isTeam1 = false, string? customName = null, double rating = -1)
    {
        Players = players;
        IsTeam1 = isTeam1;
        _customName = customName;
        if (rating is not -1) //premade teams have their own ratings
        {
            Rating = rating;
            Premade = true;
        }
    }

    public List<Player> Players { get; set; }
    public double Rating { get; set; }
    public bool Premade { get; set; }
    public List<User> Users => Players.ConvertAll(p => p.User);
    public User Captain => Players[0].User;
    public bool IsTeam1 { get; set; }

    public int ChannelRenamedCount { get; set; } = 0;
    public DiscordChannel TextChannel { get; set; }
    public DiscordChannel? VoiceChannel { get; set; }

    public string Name => _customName ?? ComputedName;
    private readonly string? _customName;
    private string ComputedName => string.IsNullOrWhiteSpace(Captain.TeamName)
        ? $"Team {(IsTeam1 ? "1" : "2")} - {Captain.InGameName}"
        : Captain.TeamName;

    public DiscordMessage DiscordMessage { get; set; }

    public record Step(TokenEntity Entity, DraftAction Action, Ulid StepId);

    public List<Step> Picks { get; set; } = [];
    public void Pick(TokenEntity entity, DraftStep draftStep)
    {
        Step? existing = Picks.FirstOrDefault(p => p.StepId == draftStep.Id);
        if (existing is not null) Picks.Remove(existing);

        Picks.Add(new Step(entity, draftStep.Action, draftStep.Id));
    }

    public List<Step> Bans { get; set; } = [];
    public void Ban(TokenEntity entity, DraftStep draftStep)
    {
        Step? existing = Bans.FirstOrDefault(p => p.StepId == draftStep.Id);
        if (existing is not null) Bans.Remove(existing);

        Bans.Add(new Step(entity, draftStep.Action, draftStep.Id));
    }

    public void MakeCaptain(User user) => MakeCaptain(user.DiscordId);
    public void MakeCaptain(DiscordUser discordUser) => MakeCaptain(discordUser.Id);
    public void MakeCaptain(ulong discordId)
    {
        Player player = Players.First(p => p.User.DiscordId == discordId);
        int index = Players.IndexOf(player);
        Players.RemoveAt(index);
        Players.Insert(0, player);
    }

    public void Sub(DiscordUser afk, User sub) => Sub(afk.Id, sub);
    public void Sub(User afk, User sub) => Sub(afk.DiscordId, sub);
    public void Sub(ulong afkId, User sub)
    {
        Player player = Players.First(p => p.User.DiscordId == afkId);
        int index = Players.IndexOf(player);
        Players.Remove(player);
        Players.Insert(index, new Player(sub));
    }

    public async Task CreateVoiceChannel(DiscordGuild guild, Team enemyTeam, ulong category)
    {
        VoiceChannel = await guild.CreateVoiceChannelAsync(Name, guild.GetChannel(category));

        foreach (DiscordMember member in enemyTeam.Users.Where(u => !u.IsTestUser)
                     .Select(user => guild.Members[user.DiscordId]))
        {
            await VoiceChannel.AddOverwriteAsync(member, deny: Permissions.AccessChannels);
        }
    }
}
