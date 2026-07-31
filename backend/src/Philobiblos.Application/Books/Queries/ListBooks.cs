using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Philobiblos.Application.Books.Dtos;
using Philobiblos.Application.Common;
using Philobiblos.Domain.Repositories;

namespace Philobiblos.Application.Books.Queries;

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

public sealed class ListBooksQueryHandler : IQueryHandler<ListBooksQuery, PagedResult<BookResponse>>
{
    private readonly IBookRepository _bookRepository;

    public ListBooksQueryHandler(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<PagedResult<BookResponse>> Handle(ListBooksQuery query, CancellationToken cancellationToken)
    {
        var paged = await _bookRepository.ListBooksAsync(
            query.Title,
            query.AuthorId,
            query.GenreId,
            query.Sort,
            query.Direction,
            query.Page ?? 1,
            query.PageSize ?? 20,
            cancellationToken);

        return new PagedResult<BookResponse>(
            paged.Items.Select(BookMapping.ToResponse).ToList(),
            paged.Page,
            paged.PageSize,
            paged.TotalCount);
    }
}
