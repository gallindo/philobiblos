using Philobiblos.Application.Authors.Dtos;
using Philobiblos.Application.Common;
using Philobiblos.Domain.Exceptions;
using Philobiblos.Domain.Repositories;

namespace Philobiblos.Application.Authors.Queries;

public sealed record GetAuthorQuery(Guid Id);

public sealed class GetAuthorQueryHandler : IQueryHandler<GetAuthorQuery, AuthorResponse>
{
    private readonly IAuthorRepository _authorRepository;

    public GetAuthorQueryHandler(IAuthorRepository authorRepository)
    {
        _authorRepository = authorRepository;
    }

    public async Task<AuthorResponse> Handle(GetAuthorQuery query, CancellationToken cancellationToken)
    {
        var author = await _authorRepository.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException($"Author '{query.Id}' was not found.");

        return AuthorMapping.ToResponse(author);
    }
}
