using BCL.Domain.Entities.Abstract;
using BCL.Domain.Enums;

namespace BCL.Domain.Entities.Queue;

public class Map : TokenEntity
{
    public MapVariant Variant { get; set; }
    public bool Pro { get; set; }
    public int Frequency { get; set; }
    public int GamesPlayed { get; set; }
}
