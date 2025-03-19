namespace BCL.Common.Extensions;

public static class StringExtensions
{
    public static string FirstCharToUpper(this string input) =>
        input switch
        {
            null => throw new ArgumentNullException(nameof(input)),
            "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
            _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
        };

    /// <summary>
    /// Returns the changes made to a champion or map serialized as json, don't use a " , " character with this
    /// </summary>
    /// <param name="oldJson">old</param>
    /// <param name="newJson">new</param>
    /// <returns></returns>
    public static string Diff(this string oldJson, string newJson)
    {
        string[] oldLines = oldJson.Split(",").Skip(1).SkipLast(1).ToArray();
        string[] newLines = newJson.Split(",").Skip(1).SkipLast(1).ToArray();

        if (oldLines.Length != newLines.Length) return "Invalid Json";

        string diff = "";

        for (int i = 0; i < oldLines.Length; i++)
        {
            if (oldLines[i] != newLines[i])
            {
                diff += $"\n- {oldLines[i].Trim()}\n+ {newLines[i].Trim()}";
            }
        }

        return diff.Trim();
    }
}
