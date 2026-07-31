using Philobiblos.Application.Books.Dtos;
using Philobiblos.Application.Common;
using Philobiblos.Domain.Exceptions;
using Philobiblos.Domain.Repositories;

namespace Philobiblos.Application.Books.Queries;

public sealed record GetBookQuery(Guid Id);

public sealed class GetBookQueryHandler : IQueryHandler<GetBookQuery, BookResponse>
{
    private readonly IBookRepository _bookRepository;

    public GetBookQueryHandler(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<BookResponse> Handle(GetBookQuery query, CancellationToken cancellationToken)
    {
        var book = await _bookRepository.GetByIdWithDetailsAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException($"Book '{query.Id}' was not found.");

        return BookMapping.ToResponse(book);
    }
}
