using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Philobiblos.Application.Common;
using Philobiblos.Application.Genres.Dtos;
using Philobiblos.Domain.Repositories;

namespace Philobiblos.Application.Genres.Queries;

public sealed record ListGenresQuery : PagedQuery
{
    [FromQuery(Name = "name")]
    public string? Name { get; init; }
}

public sealed class ListGenresQueryValidator : PagedQueryValidator<ListGenresQuery>
{
    public ListGenresQueryValidator()
    {
        RuleFor(query => query.Sort)
            .Must(sort => string.Equals(sort, "name", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Sort must be one of: name.")
            .When(query => query.Sort is not null);
    }
}

public sealed class ListGenresQueryHandler : IQueryHandler<ListGenresQuery, PagedResult<GenreResponse>>
{
    private readonly IGenreRepository _genreRepository;

    public ListGenresQueryHandler(IGenreRepository genreRepository)
    {
        _genreRepository = genreRepository;
    }

    public async Task<PagedResult<GenreResponse>> Handle(ListGenresQuery query, CancellationToken cancellationToken)
    {
        var paged = await _genreRepository.ListGenresAsync(
            query.Name,
            query.Sort,
            query.Direction,
            query.Page ?? 1,
            query.PageSize ?? 20,
            cancellationToken);

        return new PagedResult<GenreResponse>(
            paged.Items.Select(GenreMapping.ToResponse).ToList(),
            paged.Page,
            paged.PageSize,
            paged.TotalCount);
    }
}
