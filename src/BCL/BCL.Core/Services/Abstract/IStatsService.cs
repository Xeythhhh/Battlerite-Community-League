using BCL.Domain.Entities.Matches;

namespace BCL.Core.Services.Abstract;

public interface IStatsService
{
    Task UpdateStats(Match match);
    Task StartupRoutine();
}
