using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Api.Data;
using Philobiblos.Api.Domain;
using Philobiblos.Api.Infrastructure;

namespace Philobiblos.Api.Features.Genres;

public sealed record CreateGenreRequest(string Name);

public sealed class CreateGenreValidator : AbstractValidator<CreateGenreRequest>
{
    public CreateGenreValidator()
    {
        RuleFor(request => request.Name)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Name is required.")
            .Must(name => (name ?? string.Empty).Trim().Length <= 100)
            .WithMessage("Name must be 100 characters or fewer.");
    }
}

public static class CreateGenre
{
    public static RouteHandlerBuilder MapCreateGenre(this RouteGroupBuilder group) =>
        group.MapPost("/", Handle)
            .AddEndpointFilter<ValidationFilter<CreateGenreRequest>>()
            .WithName("CreateGenre")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

    private static async Task<CreatedAtRoute<GenreResponse>> Handle(
        CreateGenreRequest request,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        var nameTaken = await db.Genres.AnyAsync(
            genre => genre.Name.ToLower() == name.ToLower(),
            cancellationToken);
        if (nameTaken)
        {
            throw new ConflictException($"A genre named '{name}' already exists.");
        }

        var genre = new Genre { Id = Guid.CreateVersion7(), Name = name };
        db.Genres.Add(genre);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.CreatedAtRoute(
            GenreMapping.ToResponse(genre),
            routeName: "GetGenre",
            routeValues: new { id = genre.Id });
    }
}
