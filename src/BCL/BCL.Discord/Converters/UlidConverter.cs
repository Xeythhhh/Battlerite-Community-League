using DSharpPlus.ButtonCommands;
using DSharpPlus.ButtonCommands.Converters;
using DSharpPlus.Entities;
using DSharpPlus.ModalCommands;
using DSharpPlus.ModalCommands.Converters;

namespace BCL.Discord.Converters;

public class UlidConverter : IModalArgumentConverter<Ulid>, IButtonArgumentConverter<Ulid>
{
    public Task<Optional<Ulid>> ConvertAsync(string value, ModalContext ctx) => _Convert(value);
    public Task<Optional<Ulid>> ConvertAsync(string value, ButtonContext ctx) => _Convert(value);

    private static Task<Optional<Ulid>> _Convert(string value)
        => Task.FromResult(Ulid.TryParse(value, out Ulid res)
            ? Optional.FromValue(res)
            : Optional.FromNoValue<Ulid>());

    public string ConvertToString(Ulid value) => value.ToString();
}
