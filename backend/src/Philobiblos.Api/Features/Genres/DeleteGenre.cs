using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Api.Data;
using Philobiblos.Api.Infrastructure;

namespace Philobiblos.Api.Features.Genres;

public static class DeleteGenre
{
    public static RouteHandlerBuilder MapDeleteGenre(this RouteGroupBuilder group) =>
        group.MapDelete("/{id}", Handle)
            .WithName("DeleteGenre")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<NoContent> Handle(
        Guid id,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        var genre = await db.Genres.FirstOrDefaultAsync(genre => genre.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Genre '{id}' was not found.");

        var inUse = await db.Books.AnyAsync(book => book.GenreId == id, cancellationToken);
        if (inUse)
        {
            throw new ConflictException($"Genre '{id}' is in use by one or more books and cannot be deleted.");
        }

        db.Genres.Remove(genre);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
