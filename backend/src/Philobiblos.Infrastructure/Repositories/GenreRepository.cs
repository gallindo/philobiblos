using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Application.Common;
using Philobiblos.Domain.Common;
using Philobiblos.Domain.Entities;
using Philobiblos.Domain.Repositories;
using Philobiblos.Infrastructure.Data;
using Philobiblos.Infrastructure.Paging;

namespace Philobiblos.Infrastructure.Repositories;

public sealed class GenreRepository : Repository<Genre>, IGenreRepository
{
    public GenreRepository(LibraryDbContext context) : base(context)
    {
    }

    public Task<bool> IsNameTakenAsync(string name, Guid? excludingId = null, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLower();
        var query = Context.Genres.AsNoTracking();

        if (excludingId.HasValue)
        {
            query = query.Where(genre => genre.Id != excludingId.Value);
        }

        return query.AnyAsync(genre => genre.Name.ToLower() == normalized, cancellationToken);
    }

    public Task<bool> IsGenreInUseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Context.Books.AnyAsync(book => book.GenreId == id, cancellationToken);
    }

    public Task<PagedList<Genre>> ListGenresAsync(
        string? name,
        string? sort,
        string? direction,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Genres.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(name))
        {
            var term = name.Trim().ToLower();
            query = query.Where(genre => genre.Name.ToLower().Contains(term));
        }

        return query.ApplyPaging(
            new PagedQuery { Page = page, PageSize = pageSize, Sort = sort, Direction = direction },
            SortKey,
            genre => genre.Name,
            cancellationToken);
    }

    private static Expression<Func<Genre, object>> SortKey(string sort) =>
        sort.ToLowerInvariant() switch
        {
            "name" => genre => genre.Name,
            _ => throw new InvalidOperationException($"Unsupported sort field '{sort}'."),
        };
}
