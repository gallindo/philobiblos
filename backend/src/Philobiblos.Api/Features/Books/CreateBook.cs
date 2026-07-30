using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Api.Data;
using Philobiblos.Api.Domain;
using Philobiblos.Api.Infrastructure;

namespace Philobiblos.Api.Features.Books;

public sealed record CreateBookRequest(
    string Title,
    Guid AuthorId,
    Guid GenreId,
    string? Isbn,
    int? PublishedYear);

public sealed class CreateBookValidator : AbstractValidator<CreateBookRequest>
{
    public CreateBookValidator()
    {
        RuleFor(request => request.Title)
            .Cascade(CascadeMode.Stop)
            .Must(title => !string.IsNullOrWhiteSpace(title))
            .WithMessage("Title is required.")
            .Must(title => (title ?? string.Empty).Trim().Length <= 200)
            .WithMessage("Title must be 200 characters or fewer.");

        RuleFor(request => request.AuthorId)
            .NotEmpty()
            .WithMessage("AuthorId is required.");

        RuleFor(request => request.GenreId)
            .NotEmpty()
            .WithMessage("GenreId is required.");

        RuleFor(request => request.Isbn)
            .Must(IsbnValidator.IsValid)
            .WithMessage("Isbn must be a valid ISBN-10 or ISBN-13 (hyphens and spaces are ignored).")
            .When(request => !string.IsNullOrWhiteSpace(request.Isbn));

        RuleFor(request => request.PublishedYear)
            .InclusiveBetween(1450, DateTime.UtcNow.Year)
            .WithMessage($"PublishedYear must be between 1450 and {DateTime.UtcNow.Year}.")
            .When(request => request.PublishedYear.HasValue);
    }
}

public static class CreateBook
{
    public static RouteHandlerBuilder MapCreateBook(this RouteGroupBuilder group) =>
        group.MapPost("/", Handle)
            .AddEndpointFilter<ValidationFilter<CreateBookRequest>>()
            .WithName("CreateBook")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

    private static async Task<Results<CreatedAtRoute<BookResponse>, ValidationProblem>> Handle(
        CreateBookRequest request,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        var referenceErrors = await ValidateReferences(request.AuthorId, request.GenreId, db, cancellationToken);
        if (referenceErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(
                referenceErrors,
                detail: "One or more validation errors occurred.",
                title: "Bad Request");
        }

        var isbn = string.IsNullOrWhiteSpace(request.Isbn) ? null : IsbnValidator.Normalize(request.Isbn);

        if (isbn is not null && await db.Books.AnyAsync(book => book.Isbn == isbn, cancellationToken))
        {
            throw new ConflictException($"A book with ISBN '{isbn}' already exists.");
        }

        var book = new Book
        {
            Id = Guid.CreateVersion7(),
            Title = request.Title.Trim(),
            Isbn = isbn,
            PublishedYear = request.PublishedYear,
            AuthorId = request.AuthorId,
            GenreId = request.GenreId,
        };
        db.Books.Add(book);
        await db.SaveChangesAsync(cancellationToken);

        var response = await db.Books.AsNoTracking()
            .Where(candidate => candidate.Id == book.Id)
            .ProjectToResponse()
            .FirstAsync(cancellationToken);

        return TypedResults.CreatedAtRoute(response, routeName: "GetBook", routeValues: new { id = book.Id });
    }

    internal static async Task<Dictionary<string, string[]>> ValidateReferences(
        Guid authorId,
        Guid genreId,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (!await db.Authors.AnyAsync(author => author.Id == authorId, cancellationToken))
        {
            errors["authorId"] = [$"Author '{authorId}' does not exist."];
        }

        if (!await db.Genres.AnyAsync(genre => genre.Id == genreId, cancellationToken))
        {
            errors["genreId"] = [$"Genre '{genreId}' does not exist."];
        }

        return errors;
    }
}
