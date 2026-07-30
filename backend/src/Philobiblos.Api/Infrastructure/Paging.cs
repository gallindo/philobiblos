using System.Linq.Expressions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Api.Domain;

namespace Philobiblos.Api.Infrastructure;

public record PagedQuery
{
    [FromQuery(Name = "page")]
    public int? Page { get; init; }

    [FromQuery(Name = "pageSize")]
    public int? PageSize { get; init; }

    [FromQuery(Name = "sort")]
    public string? Sort { get; init; }

    [FromQuery(Name = "direction")]
    public string? Direction { get; init; }
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public abstract class PagedQueryValidator<T> : AbstractValidator<T>
    where T : PagedQuery
{
    protected PagedQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");

        RuleFor(query => query.Direction)
            .Must(direction => string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Direction must be 'asc' or 'desc'.")
            .When(query => query.Direction is not null);

        RuleFor(query => query.Direction)
            .Null()
            .WithMessage("Direction requires a sort field.")
            .When(query => query.Sort is null);
    }
}

public static class PagingExtensions
{
    public static async Task<PagedResult<T>> ApplyPaging<T>(
        this IQueryable<T> source,
        PagedQuery query,
        Func<string, Expression<Func<T, object>>> sortKeySelector,
        Expression<Func<T, object>> defaultSortKey,
        CancellationToken cancellationToken = default)
        where T : IEntity
    {
        var page = query.Page ?? 1;
        var pageSize = query.PageSize ?? 20;

        var totalCount = await source.CountAsync(cancellationToken);

        var ordered = query.Sort is null
            ? source.OrderBy(defaultSortKey)
            : string.Equals(query.Direction, "desc", StringComparison.OrdinalIgnoreCase)
                ? source.OrderByDescending(sortKeySelector(query.Sort))
                : source.OrderBy(sortKeySelector(query.Sort));

        var items = await ordered
            .ThenBy(IdSelector<T>())
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, page, pageSize, totalCount);
    }

    private static Expression<Func<T, Guid>> IdSelector<T>()
    {
        var parameter = Expression.Parameter(typeof(T), "entity");
        return Expression.Lambda<Func<T, Guid>>(Expression.Property(parameter, nameof(IEntity.Id)), parameter);
    }
}
