using DSharpPlus.ButtonCommands;
using DSharpPlus.ButtonCommands.Converters;
using DSharpPlus.Entities;
using DSharpPlus.ModalCommands;
using DSharpPlus.ModalCommands.Converters;

namespace BCL.Discord.Converters;

public class EnumConverter<TEnum> : IModalArgumentConverter<TEnum>, IButtonArgumentConverter<TEnum>
    where TEnum : Enum
{
    public Task<Optional<TEnum>> ConvertAsync(string value, ModalContext ctx) => _Convert(value);
    public Task<Optional<TEnum>> ConvertAsync(string value, ButtonContext ctx) => _Convert(value);

    private static Task<Optional<TEnum>> _Convert(string value)
        => Task.FromResult(Enum.TryParse(typeof(TEnum), value, true, out object? res)
            ? Optional.FromValue((TEnum)res)
            : Optional.FromNoValue<TEnum>());

    public string ConvertToString(TEnum value) => value.ToString();
}
