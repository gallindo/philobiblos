using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Application.Common;
using Philobiblos.Domain.Common;
using Philobiblos.Domain.Entities;

namespace Philobiblos.Infrastructure.Paging;

public static class PagingExtensions
{
    public static async Task<PagedList<T>> ApplyPaging<T>(
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

        return new PagedList<T>(items, page, pageSize, totalCount);
    }

    private static Expression<Func<T, Guid>> IdSelector<T>()
    {
        var parameter = Expression.Parameter(typeof(T), "entity");
        return Expression.Lambda<Func<T, Guid>>(Expression.Property(parameter, nameof(IEntity.Id)), parameter);
    }
}
