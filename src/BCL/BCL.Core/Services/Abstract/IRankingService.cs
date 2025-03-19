using BCL.Domain.Entities.Matches;

namespace BCL.Core.Services.Abstract;

public interface IRankingService
{
    Task<double> UpdateElo(Match match);
}
