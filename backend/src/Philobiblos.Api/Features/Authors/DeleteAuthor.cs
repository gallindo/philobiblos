using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Api.Data;
using Philobiblos.Api.Infrastructure;

namespace Philobiblos.Api.Features.Authors;

public static class DeleteAuthor
{
    public static RouteHandlerBuilder MapDeleteAuthor(this RouteGroupBuilder group) =>
        group.MapDelete("/{id}", Handle)
            .WithName("DeleteAuthor")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<NoContent> Handle(
        Guid id,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        var author = await db.Authors.FirstOrDefaultAsync(author => author.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Author '{id}' was not found.");

        var inUse = await db.Books.AnyAsync(book => book.AuthorId == id, cancellationToken);
        if (inUse)
        {
            throw new ConflictException($"Author '{id}' is in use by one or more books and cannot be deleted.");
        }

        db.Authors.Remove(author);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
