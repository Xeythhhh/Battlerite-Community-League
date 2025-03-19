using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BCL.Persistence.Sqlite.Converters;

/// <summary>
/// Converts Ulid to BytesArray
/// </summary>
/// <param name="mappingHints"></param>
public class UlidToBytesConverter(ConverterMappingHints? mappingHints = null) : ValueConverter<Ulid, byte[]>(
        x => x.ToByteArray(),
        x => x.Length == 0 ? default : new Ulid(x),
        DefaultHints.With(mappingHints))
{
    static readonly ConverterMappingHints DefaultHints = new(16);
}
