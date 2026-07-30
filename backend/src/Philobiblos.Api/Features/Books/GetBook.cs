using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Api.Data;
using Philobiblos.Api.Infrastructure;

namespace Philobiblos.Api.Features.Books;

public static class GetBook
{
    public static RouteHandlerBuilder MapGetBook(this RouteGroupBuilder group) =>
        group.MapGet("/{id}", Handle)
            .WithName("GetBook")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Ok<BookResponse>> Handle(
        Guid id,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        var book = await db.Books.AsNoTracking()
            .Where(candidate => candidate.Id == id)
            .ProjectToResponse()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Book '{id}' was not found.");

        return TypedResults.Ok(book);
    }
}
