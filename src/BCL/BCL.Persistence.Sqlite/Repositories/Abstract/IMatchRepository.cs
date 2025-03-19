using BCL.Domain.Entities.Matches;

namespace BCL.Persistence.Sqlite.Repositories.Abstract;
public interface IMatchRepository : IGenericRepository<Match>
{
    IEnumerable<Match> GetAllCompleted();
    IEnumerable<Match> GetAllCompleted(string season);
}
