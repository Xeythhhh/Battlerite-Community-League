using System.Reflection;

namespace BCL.Core;

public sealed class CoreAssembly
{
    /// <summary>
    /// Returns the Core Assembly
    /// </summary>
    public static Assembly Value => typeof(CoreAssembly).Assembly;
}