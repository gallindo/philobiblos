using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Api.Data;
using Philobiblos.Api.Infrastructure;

namespace Philobiblos.Api.Features.Books;

public static class DeleteBook
{
    public static RouteHandlerBuilder MapDeleteBook(this RouteGroupBuilder group) =>
        group.MapDelete("/{id}", Handle)
            .WithName("DeleteBook")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<NoContent> Handle(
        Guid id,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        var book = await db.Books.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Book '{id}' was not found.");

        db.Books.Remove(book);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
