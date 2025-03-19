namespace BCL.Common.Extensions;
public static class UriExtensions
{
    public static string DiscordLink(this string uri, string label, string tooltip) => DiscordLink(new Uri(uri), label, tooltip);
    public static string DiscordLink(this string uri, string label) => DiscordLink(new Uri(uri), label);
    public static string DiscordLink(this Uri uri, string label, string tooltip) => $"""[{label}]({uri} "{tooltip}")""";
    public static string DiscordLink(this Uri uri, string label) => $"[{label}]({uri})";
}
