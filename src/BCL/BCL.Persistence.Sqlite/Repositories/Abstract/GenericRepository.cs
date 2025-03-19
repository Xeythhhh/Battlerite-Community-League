using System.Linq.Expressions;

using BCL.Domain.Entities.Abstract;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BCL.Persistence.Sqlite.Repositories.Abstract;

public abstract class GenericRepository<TEntity> : IGenericRepository<TEntity>
    where TEntity : class, IEntity
{
    protected readonly BclDbContext DbContext;
    protected readonly DbSet<TEntity> DbSet;

    protected GenericRepository(BclDbContext dbContext)
    {
        DbContext = dbContext;
        DbSet = DbContext.Set<TEntity>();
    }

    public virtual int Count() => DbSet.Count();

    /// <summary>
    /// Counts the number of entities which satisfy this expression
    /// </summary>
    /// <param name="expression">Can not use computed properties for this expression</param>
    /// <returns></returns>
    public virtual int Count(Expression<Func<TEntity, bool>> expression) => DbSet.Count(expression);
    public virtual IEnumerable<TEntity> GetAll() => DbSet;
    public virtual TEntity? GetById(Ulid id) => DbSet.Find(id);
    public virtual TEntity? GetById(string id) => DbSet.Find(Ulid.Parse(id));
    public virtual async ValueTask<TEntity?> GetByIdAsync(Ulid id) => await DbSet.FindAsync(id);
    public virtual async ValueTask<TEntity?> GetByIdAsync(string id) => await DbSet.FindAsync(Ulid.Parse(id));
    public virtual IEnumerable<TEntity> Get(Expression<Func<TEntity, bool>> predicate) => DbSet.Where(predicate);
    public virtual EntityEntry<TEntity> Add(TEntity entity) => DbSet.Add(entity);
    public virtual void AddRange(IEnumerable<TEntity> entities) => DbSet.AddRange(entities);
    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities) => await DbSet.AddRangeAsync(entities);
    public virtual async ValueTask<EntityEntry<TEntity>> AddAsync(TEntity entity) => await DbSet.AddAsync(entity);
    public virtual EntityEntry<TEntity> Update(TEntity entity) => DbSet.Update(entity);
    public virtual EntityEntry<TEntity> Delete(Ulid id) => DbSet.Remove(DbSet.Find(id)!);
    public virtual int SaveChanges() => DbContext.SaveChanges();
    public virtual async Task SaveChangesAsync() => await DbContext.SaveChangesAsync();
    public void Dispose() => DbContext.Dispose();
}
