using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Philobiblos.Application.Authors.Dtos;
using Philobiblos.Application.Common;
using Philobiblos.Domain.Repositories;

namespace Philobiblos.Application.Authors.Queries;

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

public sealed class ListAuthorsQueryHandler : IQueryHandler<ListAuthorsQuery, PagedResult<AuthorResponse>>
{
    private readonly IAuthorRepository _authorRepository;

    public ListAuthorsQueryHandler(IAuthorRepository authorRepository)
    {
        _authorRepository = authorRepository;
    }

    public async Task<PagedResult<AuthorResponse>> Handle(ListAuthorsQuery query, CancellationToken cancellationToken)
    {
        var paged = await _authorRepository.ListAuthorsAsync(
            query.Name,
            query.Sort,
            query.Direction,
            query.Page ?? 1,
            query.PageSize ?? 20,
            cancellationToken);

        return new PagedResult<AuthorResponse>(
            paged.Items.Select(AuthorMapping.ToResponse).ToList(),
            paged.Page,
            paged.PageSize,
            paged.TotalCount);
    }
}
