using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BCL.Persistence.Sqlite.Converters;

/// <summary>
/// Converts Ulid to string
/// </summary>
/// <param name="mappingHints"></param>
public class UlidToStringConverter(ConverterMappingHints? mappingHints = null) : ValueConverter<Ulid, string>(
        ulid => ulid.ToString(),
        base32 => base32.Length == 0 ? default : Ulid.Parse(base32),
        DefaultHints.With(mappingHints))
{
    static readonly ConverterMappingHints DefaultHints = new(26);
}
