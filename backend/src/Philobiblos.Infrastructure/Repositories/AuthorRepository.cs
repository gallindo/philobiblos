using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Domain.Common;
using Philobiblos.Domain.Entities;
using Philobiblos.Domain.Repositories;
using Philobiblos.Infrastructure.Data;
using Philobiblos.Infrastructure.Paging;
using Philobiblos.Application.Common;

namespace Philobiblos.Infrastructure.Repositories;

public sealed class AuthorRepository : Repository<Author>, IAuthorRepository
{
    public AuthorRepository(LibraryDbContext context) : base(context)
    {
    }

    public Task<bool> IsNameTakenAsync(string name, Guid? excludingId = null, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLower();
        var query = Context.Authors.AsNoTracking();

        if (excludingId.HasValue)
        {
            query = query.Where(author => author.Id != excludingId.Value);
        }

        return query.AnyAsync(author => author.Name.ToLower() == normalized, cancellationToken);
    }

    public Task<bool> IsAuthorInUseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Context.Books.AnyAsync(book => book.AuthorId == id, cancellationToken);
    }

    public Task<PagedList<Author>> ListAuthorsAsync(
        string? name,
        string? sort,
        string? direction,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Authors.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(name))
        {
            var term = name.Trim().ToLower();
            query = query.Where(author => author.Name.ToLower().Contains(term));
        }

        return query.ApplyPaging(
            new PagedQuery { Page = page, PageSize = pageSize, Sort = sort, Direction = direction },
            SortKey,
            author => author.Name,
            cancellationToken);
    }

    private static Expression<Func<Author, object>> SortKey(string sort) =>
        sort.ToLowerInvariant() switch
        {
            "name" => author => author.Name,
            _ => throw new InvalidOperationException($"Unsupported sort field '{sort}'."),
        };
}
