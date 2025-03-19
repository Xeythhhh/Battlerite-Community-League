using BCL.Domain;

namespace BCL.Discord.Extensions;
public static class DoubleExtensions
{
    public static string ToGuildCurrencyString(this double value) =>
        value.ToString($"{DomainConfig.Currency.Symbol}##,###.00");
}
