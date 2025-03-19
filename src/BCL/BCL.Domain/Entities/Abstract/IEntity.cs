namespace BCL.Domain.Entities.Abstract;

public interface IEntity
{
    public Ulid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}