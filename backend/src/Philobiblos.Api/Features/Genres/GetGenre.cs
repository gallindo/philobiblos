using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Api.Data;
using Philobiblos.Api.Infrastructure;

namespace Philobiblos.Api.Features.Genres;

public static class GetGenre
{
    public static RouteHandlerBuilder MapGetGenre(this RouteGroupBuilder group) =>
        group.MapGet("/{id}", Handle)
            .WithName("GetGenre")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Ok<GenreResponse>> Handle(
        Guid id,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        var genre = await db.Genres.AsNoTracking().FirstOrDefaultAsync(genre => genre.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Genre '{id}' was not found.");

        return TypedResults.Ok(GenreMapping.ToResponse(genre));
    }
}
