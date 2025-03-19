using System.ComponentModel.DataAnnotations.Schema;

using BCL.Domain.Entities.Abstract;

namespace BCL.Domain.Entities.Users;
public class PremadeTeam : Entity
{
    #region Garbage

    //todo rework xdd
    public static int[] TeamSizes { get; } = [3];

    #endregion

    #region Properties

    public int Size { get; set; } = 3;
    public string Name { get; set; } = string.Empty;
    public string MemberIds { get; set; } = string.Empty;
    public double Rating { get; set; } = 1000;
    public bool IsRated { get; set; } = false;
    #endregion

    [NotMapped] public List<User> Members { get; set; } = [];
    [NotMapped] public User Captain => Members[0];
    [NotMapped] public bool IsValid => Members.Count == Size;
}
