using System.ComponentModel.DataAnnotations.Schema;

using BCL.Domain.Entities.Abstract;
using BCL.Domain.Enums;

namespace BCL.Domain.Entities.Matches;

public class DraftStep : Entity
{
    public DraftAction Action { get; set; }
    public Ulid TokenId1 { get; set; }
    public Ulid TokenId2 { get; set; }
    public int Index { get; set; }

    public bool IsCurrent { get; private set; }

    /// <summary>
    /// This is only used in sequential draft
    /// </summary>
    /// <returns></returns>
    public bool IsLastPick { get; set; } = false;
    [NotMapped] public bool IsConcluded => TokenId1 != default && TokenId2 != default;
    [NotMapped] public bool IsNew => TokenId1 == default && TokenId2 == default;

    /// <summary>
    /// Do not call this before starting the next step or saving a reference to .NextStep, .NextStep will be null as it is determined by the current step
    /// </summary>
    public void Finish() => IsCurrent = false;
    public void Start() => IsCurrent = true;
}
