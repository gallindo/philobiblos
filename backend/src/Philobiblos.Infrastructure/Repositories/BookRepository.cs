using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Application.Common;
using Philobiblos.Domain.Common;
using Philobiblos.Domain.Entities;
using Philobiblos.Domain.Repositories;
using Philobiblos.Infrastructure.Data;
using Philobiblos.Infrastructure.Paging;

namespace Philobiblos.Infrastructure.Repositories;

public sealed class BookRepository : Repository<Book>, IBookRepository
{
    public BookRepository(LibraryDbContext context) : base(context)
    {
    }

    public Task<bool> IsIsbnTakenAsync(string isbn, Guid? excludingId = null, CancellationToken cancellationToken = default)
    {
        var query = Context.Books.AsNoTracking();

        if (excludingId.HasValue)
        {
            query = query.Where(book => book.Id != excludingId.Value);
        }

        return query.AnyAsync(book => book.Isbn == isbn, cancellationToken);
    }

    public Task<Book?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Context.Books.AsNoTracking()
            .Include(book => book.Author)
            .Include(book => book.Genre)
            .FirstOrDefaultAsync(book => book.Id == id, cancellationToken);
    }

    public Task<PagedList<Book>> ListBooksAsync(
        string? title,
        Guid? authorId,
        Guid? genreId,
        string? sort,
        string? direction,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Book> query = Context.Books.AsNoTracking()
            .Include(book => book.Author)
            .Include(book => book.Genre);

        if (!string.IsNullOrWhiteSpace(title))
        {
            var term = title.Trim().ToLower();
            query = query.Where(book => book.Title.ToLower().Contains(term));
        }

        if (authorId.HasValue)
        {
            query = query.Where(book => book.AuthorId == authorId.Value);
        }

        if (genreId.HasValue)
        {
            query = query.Where(book => book.GenreId == genreId.Value);
        }

        return query.ApplyPaging(
            new PagedQuery { Page = page, PageSize = pageSize, Sort = sort, Direction = direction },
            SortKey,
            book => book.Title,
            cancellationToken);
    }

    private static Expression<Func<Book, object>> SortKey(string sort) =>
        sort.ToLowerInvariant() switch
        {
            "title" => book => book.Title,
            "publishedyear" => book => book.PublishedYear!,
            _ => throw new InvalidOperationException($"Unsupported sort field '{sort}'."),
        };
}
