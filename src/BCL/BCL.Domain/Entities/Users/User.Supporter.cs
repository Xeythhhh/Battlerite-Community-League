namespace BCL.Domain.Entities.Users;

public partial class User
{
    public bool Vip { get; set; } = false;
    public string TeamName { get; set; } = string.Empty;
    public ulong EmojiId { get; set; } = default;

    #region Roles
    public ulong RoleId { get; set; } = default;
    public ulong SecondaryRoleId { get; set; } = default;
    public string RoleName { get; set; } = string.Empty;
    public string RoleSuffix { get; set; } = string.Empty;
    public string RoleColor { get; set; } = string.Empty;
    public string RoleIconUrl { get; set; } = string.Empty;
    #endregion

    #region Channel
    public ulong ChannelId { get; set; } = default;
    public string ChannelName { get; set; } = string.Empty;
    #endregion

    #region Embed
    public string ProfileColor { get; set; } = DomainConfig.Profile.DefaultColor;
    public string Bio { get; set; } = string.Empty;
    public string Styling { get; set; } = string.Empty;
    public bool DisplayBothCharts { get; set; }
    #endregion

    #region Chart
    public double ChartAlpha { get; set; } = DomainConfig.Profile.DefaultChartAlpha;
    // ReSharper disable InconsistentNaming
    public string Chart_MainRatingColor { get; set; } = "#18FF00FF";
    public string Chart_SecondaryRatingColor { get; set; } = "#CAD91CFF";
    public string Chart_MainWinrateColor { get; set; } = "#328529B3";
    public string Chart_SecondaryWinrateColor { get; set; } = "#CAD91CAA";
    // ReSharper restore InconsistentNaming
    #endregion
}
