using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BCL.Domain.Entities.Abstract;
public abstract class Entity : IEntity
{
    protected Entity()
    {
        Id = Ulid.NewUlid();
    }

    [Key]
    [ScaffoldColumn(false)]
    public Ulid Id { get; set; }

    //This exists to place entitites in a certain timeframe without disrupting existing logic
    public DateTime? DateOverride { get; set; } //TODO: might be smart to have this be init-only
    [NotMapped] public DateTime RecordedAt => DateOverride ?? CreatedAt;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedAt { get; set; }
}
