using System.Linq.Expressions;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Api.Data;
using Philobiblos.Api.Domain;
using Philobiblos.Api.Infrastructure;

namespace Philobiblos.Api.Features.Authors;

public sealed record ListAuthorsQuery : PagedQuery
{
    [FromQuery(Name = "name")]
    public string? Name { get; init; }
}

public sealed class ListAuthorsQueryValidator : PagedQueryValidator<ListAuthorsQuery>
{
    public ListAuthorsQueryValidator()
    {
        RuleFor(query => query.Sort)
            .Must(sort => string.Equals(sort, "name", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Sort must be one of: name.")
            .When(query => query.Sort is not null);
    }
}

public static class ListAuthors
{
    public static RouteHandlerBuilder MapListAuthors(this RouteGroupBuilder group) =>
        group.MapGet("/", Handle)
            .AddEndpointFilter<ValidationFilter<ListAuthorsQuery>>()
            .WithName("ListAuthors")
            .ProducesValidationProblem();

    private static async Task<Ok<PagedResult<AuthorResponse>>> Handle(
        [AsParameters] ListAuthorsQuery query,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        var authors = db.Authors.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var term = query.Name.Trim().ToLower();
            authors = authors.Where(author => author.Name.ToLower().Contains(term));
        }

        var paged = await authors.ApplyPaging(query, SortKey, author => author.Name, cancellationToken);

        return TypedResults.Ok(new PagedResult<AuthorResponse>(
            paged.Items.Select(AuthorMapping.ToResponse).ToList(),
            paged.Page,
            paged.PageSize,
            paged.TotalCount));
    }

    private static Expression<Func<Author, object>> SortKey(string sort) =>
        sort.ToLowerInvariant() switch
        {
            "name" => author => author.Name,
            _ => throw new InvalidOperationException($"Unsupported sort field '{sort}'."),
        };
}
