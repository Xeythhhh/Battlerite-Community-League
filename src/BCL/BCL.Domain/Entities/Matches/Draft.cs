using System.ComponentModel.DataAnnotations.Schema;

using BCL.Domain.Entities.Abstract;
using BCL.Domain.Enums;

#pragma warning disable CS8618
namespace BCL.Domain.Entities.Matches;

public class Draft : Entity
{
    public ulong Captain1DiscordId { get; set; }
    public ulong Captain2DiscordId { get; set; }
    public DraftType DraftType { get; set; } = 0;

    public virtual List<DraftStep> Steps { get; set; }

    /// <summary>
    /// This is only for sequential draft
    /// </summary>
    public bool IsTeam1Turn { get; set; } = true;
    /// <summary>
    /// Number of actions before it's enemy team's turn to pick/ban
    /// </summary>
    public int RemainingActions { get; set; }

    [NotMapped] public bool IsFinished => Steps.All(s => s.IsConcluded);
    [NotMapped]
    public DraftStep? CurrentStep
    {
        get
        {
            try
            {
                return Steps.SingleOrDefault(s => s.IsCurrent);
            }
            catch (InvalidOperationException)
            {
                List<DraftStep> currentSteps = Steps.Where(s => s.IsCurrent).ToList();
                currentSteps.ForEach(s =>
                {
                    if (s.IsConcluded) s.Finish();
                });

                return Steps.ElementAtOrDefault(Steps.IndexOf(Steps.Last(s => s.IsConcluded)) + 1);
            }
        }
    }

    [NotMapped] public DraftStep? NextStep => Steps.ElementAtOrDefault(Steps.IndexOf(CurrentStep ?? Steps[^1]) + 1);
}
