using System.Reflection;

namespace BCL.Domain;

public sealed class DomainAssembly
{
    /// <summary>
    /// Returns the Domain Assembly
    /// </summary>
    public static Assembly Value => typeof(DomainAssembly).Assembly;
}