using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Api.Data;
using Philobiblos.Api.Infrastructure;

namespace Philobiblos.Api.Features.Authors;

public sealed record UpdateAuthorRequest(string Name, string? Bio);

public sealed class UpdateAuthorValidator : AbstractValidator<UpdateAuthorRequest>
{
    public UpdateAuthorValidator()
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

public static class UpdateAuthor
{
    public static RouteHandlerBuilder MapUpdateAuthor(this RouteGroupBuilder group) =>
        group.MapPut("/{id}", Handle)
            .AddEndpointFilter<ValidationFilter<UpdateAuthorRequest>>()
            .WithName("UpdateAuthor")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

    private static async Task<Ok<AuthorResponse>> Handle(
        Guid id,
        UpdateAuthorRequest request,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        var author = await db.Authors.FirstOrDefaultAsync(author => author.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Author '{id}' was not found.");

        var name = request.Name.Trim();

        var nameTaken = await db.Authors.AnyAsync(
            other => other.Id != id && other.Name.ToLower() == name.ToLower(),
            cancellationToken);
        if (nameTaken)
        {
            throw new ConflictException($"An author named '{name}' already exists.");
        }

        author.Name = name;
        author.Bio = request.Bio;
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(AuthorMapping.ToResponse(author));
    }
}
