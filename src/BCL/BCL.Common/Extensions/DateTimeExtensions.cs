using BCL.Domain.Enums;

namespace BCL.Common.Extensions;
public static class DateTimeExtensions
{
    static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0);

    public static string DiscordTime(this DateTime dateTime, DiscordTimeFlag flag = DiscordTimeFlag.f)
        => $"<t:{dateTime.UnixTimeStamp()}:{flag}>";

    static int UnixTimeStamp(this DateTime dateTime)
        => (int)(dateTime - UnixEpoch).TotalSeconds;
}
