using System.Linq.Expressions;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Api.Data;
using Philobiblos.Api.Domain;
using Philobiblos.Api.Infrastructure;

namespace Philobiblos.Api.Features.Genres;

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

public static class ListGenres
{
    public static RouteHandlerBuilder MapListGenres(this RouteGroupBuilder group) =>
        group.MapGet("/", Handle)
            .AddEndpointFilter<ValidationFilter<ListGenresQuery>>()
            .WithName("ListGenres")
            .ProducesValidationProblem();

    private static async Task<Ok<PagedResult<GenreResponse>>> Handle(
        [AsParameters] ListGenresQuery query,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        var genres = db.Genres.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var term = query.Name.Trim().ToLower();
            genres = genres.Where(genre => genre.Name.ToLower().Contains(term));
        }

        var paged = await genres.ApplyPaging(query, SortKey, genre => genre.Name, cancellationToken);

        return TypedResults.Ok(new PagedResult<GenreResponse>(
            paged.Items.Select(GenreMapping.ToResponse).ToList(),
            paged.Page,
            paged.PageSize,
            paged.TotalCount));
    }

    private static Expression<Func<Genre, object>> SortKey(string sort) =>
        sort.ToLowerInvariant() switch
        {
            "name" => genre => genre.Name,
            _ => throw new InvalidOperationException($"Unsupported sort field '{sort}'."),
        };
}
