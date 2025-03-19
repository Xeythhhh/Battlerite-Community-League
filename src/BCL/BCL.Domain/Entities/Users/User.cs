using System.ComponentModel.DataAnnotations.Schema;

using BCL.Domain.Entities.Abstract;

// ReSharper disable InconsistentNaming
#pragma warning disable CS8618
namespace BCL.Domain.Entities.Users;

public partial class User : Entity
{
    public string Name { get; set; }
    public string InGameName { get; set; }
    public ulong DiscordId { get; set; }
    public Ulid TeamId { get; set; } = Ulid.Empty;

    public bool Pro { get; set; } = false;
    public double Rating { get; set; } = DomainConfig.DefaultRating;
    public double Rating_Standard { get; set; } = DomainConfig.DefaultRating;
    public string DefaultMelee { get; set; } = string.Empty;
    public string DefaultRanged { get; set; } = string.Empty;
    public string DefaultSupport { get; set; } = string.Empty;
    public DateTime? LastQueued { get; set; }
    public DateTime? LastPlayed { get; set; }

    //Match History
    public string LatestMatch_DiscordLink_Label { get; set; } = string.Empty;
    public string LatestMatch_DiscordLink_ToolTip { get; set; } = string.Empty;
    public Uri LatestMatchLink { get; set; } = new Uri("about:blank");
    [NotMapped] public bool HasLatestMatchLink => LatestMatchLink.OriginalString != "about:blank";
    public bool MatchHistory_DisplayTournament { get; set; } = false;
    public bool MatchHistory_DisplayEvent { get; set; } = false;
    public bool MatchHistory_DisplayCustom { get; set; } = false;

    //Other
    public string Mention => $"<@{DiscordId}>";
    public string ChannelMention => $"<#{ChannelId}>";
    public string RoleMention => $"<@&{RoleId}>";
    public string SecondaryRoleMention => $"<@&{SecondaryRoleId}>";

    protected bool Equals(User other) =>
        Name == other.Name &&
        InGameName == other.InGameName &&
        DiscordId == other.DiscordId;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((User)obj);
    }
    // ReSharper disable NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => HashCode.Combine(Name, InGameName, DiscordId);
    // ReSharper restore NonReadonlyMemberInGetHashCode
}
