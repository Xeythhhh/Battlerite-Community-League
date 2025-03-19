using BCL.Domain.Enums;

namespace BCL.Domain.Entities.Users;

public partial class User
{
    public Region Server { get; set; }
    public bool Na { get; set; } = false;
    public bool Eu { get; set; } = false;
    public bool Sa { get; set; } = false;
    public bool ProQueue { get; set; } = false;
    public bool StandardQueue { get; set; } = true;
    public bool CrossRegion { get; set; } = false;
    public bool NewMatchDm { get; set; } = true;
    public int PurgeAfter { get; set; } = 0; //minutes
}
