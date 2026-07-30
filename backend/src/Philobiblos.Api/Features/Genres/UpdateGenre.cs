using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Api.Data;
using Philobiblos.Api.Infrastructure;

namespace Philobiblos.Api.Features.Genres;

public sealed record UpdateGenreRequest(string Name);

public sealed class UpdateGenreValidator : AbstractValidator<UpdateGenreRequest>
{
    public UpdateGenreValidator()
    {
        RuleFor(request => request.Name)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Name is required.")
            .Must(name => (name ?? string.Empty).Trim().Length <= 100)
            .WithMessage("Name must be 100 characters or fewer.");
    }
}

public static class UpdateGenre
{
    public static RouteHandlerBuilder MapUpdateGenre(this RouteGroupBuilder group) =>
        group.MapPut("/{id}", Handle)
            .AddEndpointFilter<ValidationFilter<UpdateGenreRequest>>()
            .WithName("UpdateGenre")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

    private static async Task<Ok<GenreResponse>> Handle(
        Guid id,
        UpdateGenreRequest request,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        var genre = await db.Genres.FirstOrDefaultAsync(genre => genre.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Genre '{id}' was not found.");

        var name = request.Name.Trim();

        var nameTaken = await db.Genres.AnyAsync(
            other => other.Id != id && other.Name.ToLower() == name.ToLower(),
            cancellationToken);
        if (nameTaken)
        {
            throw new ConflictException($"A genre named '{name}' already exists.");
        }

        genre.Name = name;
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(GenreMapping.ToResponse(genre));
    }
}
