using System.Reflection;

namespace BCL.Discord;

public sealed class DiscordAssembly
{
    /// <summary>
    /// Returns the Discord Assembly
    /// </summary>
    public static Assembly Value => typeof(DiscordAssembly).Assembly;
}