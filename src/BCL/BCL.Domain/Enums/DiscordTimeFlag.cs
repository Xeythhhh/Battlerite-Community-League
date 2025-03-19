namespace BCL.Domain.Enums;

/// <summary>
/// <para>d - Month/Day/Year</para>
/// <para>f - Month Day, Year Time</para>
/// <para>t - Time</para>
/// <para>D - Month Day, Year</para>
/// <para>F - Weekday, Month Day, Year Time</para>
/// <para>T - Hours:Minutes:Seconds</para>
/// <para>R - Time Since</para>
/// </summary>
public enum DiscordTimeFlag
{
    /// <summary>
    /// <para>Month/Day/Year</para>
    /// <para>07/10/2021</para>
    /// </summary>
    d,

    /// <summary>
    /// <para>Month Day, Year Time</para>
    /// <para>July 10, 2021 1:21 PM</para>
    /// </summary>
    f,

    /// <summary>
    /// <para>Time</para>
    /// <para>1:21 PM</para>
    /// </summary>
    t,

    /// <summary>
    /// <para>Month Day, Year</para>
    /// <para>July 10, 2021</para>
    /// </summary>
    D,

    /// <summary>
    /// <para>Weekday, Month Day, Year Time</para>
    /// <para>Saturday, July 10, 2021 1:21 PM</para>
    /// </summary>
    F,

    /// <summary>
    /// <para>Hours:Minutes:Seconds</para>
    /// <para>1:21:08 PM</para>
    /// </summary>
    T,

    /// <summary>
    /// <para>Time Since</para>
    /// <para>41 minutes ago</para>
    /// </summary>
    R,
}
