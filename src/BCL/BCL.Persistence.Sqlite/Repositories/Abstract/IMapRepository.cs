using BCL.Domain.Entities.Queue;

namespace BCL.Persistence.Sqlite.Repositories.Abstract;

public interface IMapRepository : IGenericRepository<Map>
{
    IEnumerable<Map> GetAllEnabled();
}