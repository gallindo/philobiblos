using Philobiblos.Domain.Entities;

namespace Philobiblos.Application.Genres.Dtos;

public sealed record GenreResponse(Guid Id, string Name);

public static class GenreMapping
{
    public static GenreResponse ToResponse(Genre genre) => new(genre.Id, genre.Name);
}
