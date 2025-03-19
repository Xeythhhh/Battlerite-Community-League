using System.Linq.Expressions;

using BCL.Domain.Entities.Abstract;

using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BCL.Persistence.Sqlite.Repositories.Abstract;

public interface IGenericRepository<TEntity> : IDisposable
    where TEntity : class, IEntity
{
    int Count();

    /// <summary>
    /// Counts the number of entities which satisfy this expression
    /// </summary>
    /// <param name="expression">Can not use computed properties for this expression</param>
    /// <returns></returns>
    int Count(Expression<Func<TEntity, bool>> expression);
    IEnumerable<TEntity> GetAll();
    TEntity? GetById(Ulid id);
    TEntity? GetById(string id);
    ValueTask<TEntity?> GetByIdAsync(Ulid id);
    ValueTask<TEntity?> GetByIdAsync(string id);
    IEnumerable<TEntity> Get(Expression<Func<TEntity, bool>> predicate);
    EntityEntry<TEntity> Add(TEntity entity);
    ValueTask<EntityEntry<TEntity>> AddAsync(TEntity entity);
    void AddRange(IEnumerable<TEntity> entities);
    Task AddRangeAsync(IEnumerable<TEntity> entities);
    EntityEntry<TEntity> Update(TEntity entity);
    EntityEntry<TEntity> Delete(Ulid id);
    int SaveChanges();
    Task SaveChangesAsync();
}
