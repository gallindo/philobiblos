using Philobiblos.Api.Domain;

namespace Philobiblos.Api.Features.Genres;

public sealed record GenreResponse(Guid Id, string Name);

public static class GenreMapping
{
    public static GenreResponse ToResponse(Genre genre) => new(genre.Id, genre.Name);
}
