using System.Diagnostics;

namespace BCL.Discord.Extensions;
public static class ObjectExtensions
{
    public static string FormatForDiscordCode(this object input, int length, bool start = false)
    {
        string value = input.ToString() ?? throw new UnreachableException("Make sure the object has a .ToString() implementation and is not null.");
        if (value.Length < length)
        {
            string filler = new(' ', length - value.Length);
            value = start ? $"{filler}{value}" : $"{value}{filler}";
        }
        else if (value.Length > length)
        {
            value = value[..length];
        }

        return value;
    }
}
