using System.Linq.Expressions;

using BCL.Domain.Entities.Matches;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace BCL.Persistence.Sqlite.Repositories;
public class MatchRepository(BclDbContext dbContext) : GenericRepository<Match>(dbContext), IMatchRepository
{
    public IEnumerable<Match> GetAllCompleted() => Get(m => m.Outcome != MatchOutcome.Canceled && m.Outcome != MatchOutcome.InProgress);
    public IEnumerable<Match> GetAllCompleted(string season) => GetAllCompleted().Where(m => m.Season == season);
    public override IEnumerable<Match> GetAll() => Matches;
    public override Match? GetById(Ulid id) => Matches.FirstOrDefault(m => m.Id == id);
    public override Match? GetById(string id) => Matches.FirstOrDefault(m => m.Id == Ulid.Parse(id));
    public override async ValueTask<Match?> GetByIdAsync(Ulid id) => await Matches.FirstOrDefaultAsync(m => m.Id == id);
    public override async ValueTask<Match?> GetByIdAsync(string id) => await Matches.FirstOrDefaultAsync(m => m.Id == Ulid.Parse(id));
    public override IEnumerable<Match> Get(Expression<Func<Match, bool>> predicate) => Matches.Where(predicate);

    private IIncludableQueryable<Match, List<DraftStep>> Matches => DbSet.Include(m => m.Draft).ThenInclude(d => d.Steps);
}
