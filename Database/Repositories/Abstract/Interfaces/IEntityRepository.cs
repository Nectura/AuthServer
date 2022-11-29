using System.Linq.Expressions;

namespace AuthServer.Database.Repositories.Abstract.Interfaces;

public interface IEntityRepository<T> where T : class
{
    Task<bool> AnyAsync(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default);
    Task<T?> FindAsync<TPK>(TPK primaryKey, CancellationToken cancellationToken = default);
    Task<(T entity, bool justCreated)> FindOrCreateAsync(Expression<Func<T, bool>> searchExpression, Func<T> creationMethod, CancellationToken cancellationToken = default);
    Task<List<T>> FetchAllAsync(CancellationToken cancellationToken = default);
    IQueryable<T> Query(Expression<Func<T, bool>> expression);
    T Add(T entity);
    void AddRange(IEnumerable<T> entities);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}