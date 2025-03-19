using BCL.Domain.Entities.Queue;
using BCL.Persistence.Sqlite.Repositories.Abstract;

namespace BCL.Persistence.Sqlite.Repositories;

public class MapRepository(BclDbContext dbContext) : GenericRepository<Map>(dbContext), IMapRepository
{
    public IEnumerable<Map> GetAllEnabled() => DbSet.Where(m => !m.Disabled).ToList();
}