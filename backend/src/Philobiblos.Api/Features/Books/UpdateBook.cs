using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Api.Data;
using Philobiblos.Api.Infrastructure;

namespace Philobiblos.Api.Features.Books;

public sealed record UpdateBookRequest(
    string Title,
    Guid AuthorId,
    Guid GenreId,
    string? Isbn,
    int? PublishedYear);

public sealed class UpdateBookValidator : AbstractValidator<UpdateBookRequest>
{
    public UpdateBookValidator()
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

public static class UpdateBook
{
    public static RouteHandlerBuilder MapUpdateBook(this RouteGroupBuilder group) =>
        group.MapPut("/{id}", Handle)
            .AddEndpointFilter<ValidationFilter<UpdateBookRequest>>()
            .WithName("UpdateBook")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

    private static async Task<Results<Ok<BookResponse>, ValidationProblem>> Handle(
        Guid id,
        UpdateBookRequest request,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        var book = await db.Books.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Book '{id}' was not found.");

        var referenceErrors = await CreateBook.ValidateReferences(request.AuthorId, request.GenreId, db, cancellationToken);
        if (referenceErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(
                referenceErrors,
                detail: "One or more validation errors occurred.",
                title: "Bad Request");
        }

        var isbn = string.IsNullOrWhiteSpace(request.Isbn) ? null : IsbnValidator.Normalize(request.Isbn);

        if (isbn is not null && await db.Books.AnyAsync(
                candidate => candidate.Id != id && candidate.Isbn == isbn,
                cancellationToken))
        {
            throw new ConflictException($"A book with ISBN '{isbn}' already exists.");
        }

        book.Title = request.Title.Trim();
        book.Isbn = isbn;
        book.PublishedYear = request.PublishedYear;
        book.AuthorId = request.AuthorId;
        book.GenreId = request.GenreId;
        await db.SaveChangesAsync(cancellationToken);

        var response = await db.Books.AsNoTracking()
            .Where(candidate => candidate.Id == book.Id)
            .ProjectToResponse()
            .FirstAsync(cancellationToken);

        return TypedResults.Ok(response);
    }
}
