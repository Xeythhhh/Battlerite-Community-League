namespace BCL.Common.Extensions;
public static class GenericExtensions
{
    public static bool In<T>(this T value, params T[] args) => args.Contains(value);
}
