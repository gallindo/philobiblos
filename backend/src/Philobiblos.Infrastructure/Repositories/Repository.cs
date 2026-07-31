using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Domain.Entities;
using Philobiblos.Domain.Repositories;
using Philobiblos.Infrastructure.Data;

namespace Philobiblos.Infrastructure.Repositories;

public abstract class Repository<T> : IRepository<T> where T : class, IEntity
{
    protected readonly LibraryDbContext Context;

    protected Repository(LibraryDbContext context)
    {
        Context = context;
    }

    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Context.Set<T>().FindAsync([id], cancellationToken).AsTask();
    }

    public Task<T?> GetByIdAsync(Guid id, Expression<Func<T, object>>[] includes, CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = Context.Set<T>();
        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return query.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
    }

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return Context.Set<T>().AnyAsync(predicate, cancellationToken);
    }

    public Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return Context.Set<T>().CountAsync(predicate, cancellationToken);
    }

    public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await Context.Set<T>().Where(predicate).ToListAsync(cancellationToken);
    }

    public void Add(T entity)
    {
        Context.Set<T>().Add(entity);
    }

    public void Remove(T entity)
    {
        Context.Set<T>().Remove(entity);
    }
}
