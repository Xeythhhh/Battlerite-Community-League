using System.Text.RegularExpressions;

namespace BCL.Discord.Utils;

public static partial class AnsiUtils
{
    [GeneratedRegex(@"\u001b\[[0-9;]*m", RegexOptions.Compiled)]
    private static partial Regex AnsiRegex();

    private static readonly Regex Regex = AnsiRegex();

    public static string StripAnsi(string input) => Regex.Replace(input, "");

    public static int GetVisibleLength(string input) => StripAnsi(input).Length;
}
