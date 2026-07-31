using Philobiblos.Application.Common;
using Philobiblos.Application.Genres.Dtos;
using Philobiblos.Domain.Exceptions;
using Philobiblos.Domain.Repositories;

namespace Philobiblos.Application.Genres.Queries;

public sealed record GetGenreQuery(Guid Id);

public sealed class GetGenreQueryHandler : IQueryHandler<GetGenreQuery, GenreResponse>
{
    private readonly IGenreRepository _genreRepository;

    public GetGenreQueryHandler(IGenreRepository genreRepository)
    {
        _genreRepository = genreRepository;
    }

    public async Task<GenreResponse> Handle(GetGenreQuery query, CancellationToken cancellationToken)
    {
        var genre = await _genreRepository.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException($"Genre '{query.Id}' was not found.");

        return GenreMapping.ToResponse(genre);
    }
}
