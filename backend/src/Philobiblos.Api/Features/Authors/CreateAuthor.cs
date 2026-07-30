using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Api.Data;
using Philobiblos.Api.Domain;
using Philobiblos.Api.Infrastructure;

namespace Philobiblos.Api.Features.Authors;

public sealed record CreateAuthorRequest(string Name, string? Bio);

public sealed class CreateAuthorValidator : AbstractValidator<CreateAuthorRequest>
{
    public CreateAuthorValidator()
    {
        RuleFor(request => request.Name)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Name is required.")
            .Must(name => (name ?? string.Empty).Trim().Length <= 150)
            .WithMessage("Name must be 150 characters or fewer.");

        RuleFor(request => request.Bio)
            .MaximumLength(2000)
            .WithMessage("Bio must be 2000 characters or fewer.");
    }
}

public static class CreateAuthor
{
    public static RouteHandlerBuilder MapCreateAuthor(this RouteGroupBuilder group) =>
        group.MapPost("/", Handle)
            .AddEndpointFilter<ValidationFilter<CreateAuthorRequest>>()
            .WithName("CreateAuthor")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

    internal static async Task<CreatedAtRoute<AuthorResponse>> Handle(
        CreateAuthorRequest request,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        var nameTaken = await db.Authors.AnyAsync(
            author => author.Name.ToLower() == name.ToLower(),
            cancellationToken);
        if (nameTaken)
        {
            throw new ConflictException($"An author named '{name}' already exists.");
        }

        var author = new Author { Id = Guid.CreateVersion7(), Name = name, Bio = request.Bio };
        db.Authors.Add(author);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.CreatedAtRoute(
            AuthorMapping.ToResponse(author),
            routeName: "GetAuthor",
            routeValues: new { id = author.Id });
    }
}
