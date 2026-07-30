using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Api.Data;
using Philobiblos.Api.Infrastructure;

namespace Philobiblos.Api.Features.Authors;

public static class GetAuthor
{
    public static RouteHandlerBuilder MapGetAuthor(this RouteGroupBuilder group) =>
        group.MapGet("/{id}", Handle)
            .WithName("GetAuthor")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Ok<AuthorResponse>> Handle(
        Guid id,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        var author = await db.Authors.AsNoTracking().FirstOrDefaultAsync(author => author.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Author '{id}' was not found.");

        return TypedResults.Ok(AuthorMapping.ToResponse(author));
    }
}
