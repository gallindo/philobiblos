using System.Linq.Expressions;
using Philobiblos.Domain.Common;
using Philobiblos.Domain.Entities;

namespace Philobiblos.Domain.Repositories;

public interface IRepository<T> where T : IEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<T?> GetByIdAsync(Guid id, Expression<Func<T, object>>[] includes, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    void Add(T entity);
    void Remove(T entity);
}

public interface IAuthorRepository : IRepository<Author>
{
    Task<bool> IsNameTakenAsync(string name, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task<bool> IsAuthorInUseAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedList<Author>> ListAuthorsAsync(
        string? name,
        string? sort,
        string? direction,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public interface IGenreRepository : IRepository<Genre>
{
    Task<bool> IsNameTakenAsync(string name, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task<bool> IsGenreInUseAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedList<Genre>> ListGenresAsync(
        string? name,
        string? sort,
        string? direction,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public interface IBookRepository : IRepository<Book>
{
    Task<bool> IsIsbnTakenAsync(string isbn, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task<Book?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedList<Book>> ListBooksAsync(
        string? title,
        Guid? authorId,
        Guid? genreId,
        string? sort,
        string? direction,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByProviderAsync(string provider, string providerSubject, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
