using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using AuthServer.Database.Repositories.Abstract.Interfaces;

namespace AuthServer.Database.Repositories.Abstract;

public abstract class EntityRepository<T> : IEntityRepository<T> where T : class
{
    protected readonly DbContext _context;

    protected EntityRepository(DbContext context)
    {
        _context = context;
    }

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await Query(expression).AnyAsync(cancellationToken);
    }

    public async Task<T?> FindAsync<TPK>(TPK primaryKey, CancellationToken cancellationToken = default)
    {
        return await _context.Set<T>().FindAsync(new object?[] { primaryKey }, cancellationToken: cancellationToken);
    }

    public async Task<(T entity, bool justCreated)> FindOrCreateAsync(Expression<Func<T, bool>> searchExpression, Func<T> creationMethod, CancellationToken cancellationToken = default)
    {
        var entity = await Query(searchExpression).FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (entity != default)
            return (entity, false);

        entity = Add(creationMethod.Invoke());

        await SaveChangesAsync(cancellationToken);

        return (entity, true);
    }

    public async Task<List<T>> FetchAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<T>().ToListAsync(cancellationToken);
    }

    public IQueryable<T> Query(Expression<Func<T, bool>> expression)
    {
        return _context.Set<T>().Where(expression);
    }

    public T Add(T entity)
    {
        return _context.Set<T>().Add(entity).Entity;
    }

    public void AddRange(IEnumerable<T> entities)
    {
        _context.Set<T>().AddRange(entities);
    }

    public void Remove(T entity)
    {
        _context.Set<T>().Remove(entity);
    }

    public void RemoveRange(IEnumerable<T> entities)
    {
        _context.Set<T>().RemoveRange(entities);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}