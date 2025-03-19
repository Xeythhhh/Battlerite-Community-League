#pragma warning disable CS8618
namespace BCL.Domain.Entities.Abstract;

public abstract class TokenEntity : Entity
{
    public string Name { get; set; }
    public bool Disabled { get; set; } = false;
    public bool StandardBanned { get; set; } = false;
}
