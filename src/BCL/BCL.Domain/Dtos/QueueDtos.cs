using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;

namespace BCL.Domain.Dtos;
public record Player(User User, QueueRole Role = QueueRole.Fill) : IComparable<Player>
{
    public int CompareTo(Player? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        int nameComparison = string.CompareOrdinal(User.InGameName, other.User.InGameName);
        return nameComparison != 0
            ? nameComparison
            : User.Rating.CompareTo(other.User.Rating);
    }
}

public class QueuePlayer
{
    public ulong DiscordId { get; set; }
    public QueueRole Role { get; set; }
    public List<Region> Regions { get; set; } = [];
    public List<League> Leagues { get; set; } = [];
    public bool CrossRegion { get; set; }
    public Region Server { get; set; }
    public Ulid Id { get; set; }
    public Ulid TeamId { get; set; }

    public bool Eu => Regions.Contains(Region.Eu);
    public bool Na => Regions.Contains(Region.Na);
    public bool Sa => Regions.Contains(Region.Sa);

    public bool Pro => Leagues.Contains(League.Pro);
    public bool Standard => Leagues.Contains(League.Standard);
    public bool Premade3V3 => Leagues.Contains(League.Premade3V3);

    public bool Premade => Premade3V3; // if we add more premade leagues, this will need to be reworked

    public QueuePlayer(User user, QueueRole role = QueueRole.Fill, PremadeTeam? team = null)
    {
        User? captain = team?.Captain;

        Id = user.Id;
        DiscordId = user.DiscordId;
        Role = role;

        if (captain?.Eu ?? user.Eu) Regions.Add(Region.Eu);
        if (captain?.Na ?? user.Na) Regions.Add(Region.Na);
        if (captain?.Sa ?? user.Sa) Regions.Add(Region.Sa);

        Server = captain?.Server ?? user.Server;
        CrossRegion = captain?.CrossRegion ?? user.CrossRegion;

        if (captain is null && user is { ProQueue: true, Pro: true })
            Leagues.Add(League.Pro);

        if (captain is null && user.StandardQueue)
            Leagues.Add(League.Standard);

        if (captain is not null) Leagues.Add(League.Premade3V3);

        TeamId = team?.Id ?? Ulid.Empty;
    }
}
