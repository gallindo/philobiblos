using System.Linq.Expressions;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Api.Data;
using Philobiblos.Api.Domain;
using Philobiblos.Api.Infrastructure;

namespace Philobiblos.Api.Features.Books;

public sealed record ListBooksQuery : PagedQuery
{
    [FromQuery(Name = "title")]
    public string? Title { get; init; }

    [FromQuery(Name = "authorId")]
    public Guid? AuthorId { get; init; }

    [FromQuery(Name = "genreId")]
    public Guid? GenreId { get; init; }
}

public sealed class ListBooksQueryValidator : PagedQueryValidator<ListBooksQuery>
{
    public ListBooksQueryValidator()
    {
        RuleFor(query => query.Sort)
            .Must(sort => string.Equals(sort, "title", StringComparison.OrdinalIgnoreCase)
                || string.Equals(sort, "publishedYear", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Sort must be one of: title, publishedYear.")
            .When(query => query.Sort is not null);
    }
}

public static class ListBooks
{
    public static RouteHandlerBuilder MapListBooks(this RouteGroupBuilder group) =>
        group.MapGet("/", Handle)
            .AddEndpointFilter<ValidationFilter<ListBooksQuery>>()
            .WithName("ListBooks")
            .ProducesValidationProblem();

    private static async Task<Ok<PagedResult<BookResponse>>> Handle(
        [AsParameters] ListBooksQuery query,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        var books = db.Books.AsNoTracking()
            .Include(book => book.Author)
            .Include(book => book.Genre)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            var term = query.Title.Trim().ToLower();
            books = books.Where(book => book.Title.ToLower().Contains(term));
        }

        if (query.AuthorId is { } authorId)
        {
            books = books.Where(book => book.AuthorId == authorId);
        }

        if (query.GenreId is { } genreId)
        {
            books = books.Where(book => book.GenreId == genreId);
        }

        var paged = await books.ApplyPaging(query, SortKey, book => book.Title, cancellationToken);

        return TypedResults.Ok(new PagedResult<BookResponse>(
            paged.Items.Select(BookMapping.ToResponse).ToList(),
            paged.Page,
            paged.PageSize,
            paged.TotalCount));
    }

    private static Expression<Func<Book, object>> SortKey(string sort) =>
        sort.ToLowerInvariant() switch
        {
            "title" => book => book.Title,
            "publishedyear" => book => book.PublishedYear!,
            _ => throw new InvalidOperationException($"Unsupported sort field '{sort}'."),
        };
}
