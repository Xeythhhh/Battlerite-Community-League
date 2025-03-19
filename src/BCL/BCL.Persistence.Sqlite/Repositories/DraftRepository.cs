using BCL.Domain.Entities.Matches;
using BCL.Persistence.Sqlite.Repositories.Abstract;

namespace BCL.Persistence.Sqlite.Repositories;

public class DraftRepository(BclDbContext dbContext) : GenericRepository<Draft>(dbContext), IDraftRepository
{
}