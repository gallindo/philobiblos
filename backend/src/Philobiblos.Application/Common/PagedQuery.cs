using Microsoft.AspNetCore.Mvc;

namespace Philobiblos.Application.Common;

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
