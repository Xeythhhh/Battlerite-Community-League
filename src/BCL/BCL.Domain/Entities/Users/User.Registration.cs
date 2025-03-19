namespace BCL.Domain.Entities.Users;

public partial class User
{
    public bool Approved { get; set; } = false;
    public bool IsTestUser { get; set; } = false;
    public string RegistrationInfo { get; set; } = string.Empty;
    public string ProfileVersion { get; set; } = DomainConfig.Profile.Version;
    public DateTime? Timeout { get; set; } = null;
    public string TimeoutReason { get; set; } = string.Empty;
    public DateTime? ProApplicationTimeout { get; set; }
    public string ProApplications { get; set; } = string.Empty;
}
