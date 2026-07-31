using FluentValidation;
using Philobiblos.Application.Books.Dtos;
using Philobiblos.Application.Common;
using Philobiblos.Domain.Entities;
using Philobiblos.Domain.Exceptions;
using Philobiblos.Domain.Repositories;

namespace Philobiblos.Application.Books.Commands;

public sealed record CreateBookCommand(
    string Title,
    Guid AuthorId,
    Guid GenreId,
    string? Isbn,
    int? PublishedYear);

public sealed class CreateBookCommandValidator : AbstractValidator<CreateBookCommand>
{
    public CreateBookCommandValidator()
    {
        RuleFor(command => command.Title)
            .Cascade(CascadeMode.Stop)
            .Must(title => !string.IsNullOrWhiteSpace(title))
            .WithMessage("Title is required.")
            .Must(title => (title ?? string.Empty).Trim().Length <= 200)
            .WithMessage("Title must be 200 characters or fewer.");

        RuleFor(command => command.AuthorId)
            .NotEmpty()
            .WithMessage("AuthorId is required.");

        RuleFor(command => command.GenreId)
            .NotEmpty()
            .WithMessage("GenreId is required.");

        RuleFor(command => command.Isbn)
            .Must(IsbnValidator.IsValid)
            .WithMessage("Isbn must be a valid ISBN-10 or ISBN-13 (hyphens and spaces are ignored).")
            .When(command => !string.IsNullOrWhiteSpace(command.Isbn));

        RuleFor(command => command.PublishedYear)
            .InclusiveBetween(1450, DateTime.UtcNow.Year)
            .WithMessage($"PublishedYear must be between 1450 and {DateTime.UtcNow.Year}.")
            .When(command => command.PublishedYear.HasValue);
    }
}

public sealed class CreateBookCommandHandler : ICommandHandler<CreateBookCommand, Result<BookResponse>>
{
    private readonly IBookRepository _bookRepository;
    private readonly IAuthorRepository _authorRepository;
    private readonly IGenreRepository _genreRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBookCommandHandler(
        IBookRepository bookRepository,
        IAuthorRepository authorRepository,
        IGenreRepository genreRepository,
        IUnitOfWork unitOfWork)
    {
        _bookRepository = bookRepository;
        _authorRepository = authorRepository;
        _genreRepository = genreRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BookResponse>> Handle(CreateBookCommand command, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (!await _authorRepository.AnyAsync(author => author.Id == command.AuthorId, cancellationToken))
        {
            errors["authorId"] = [$"Author '{command.AuthorId}' does not exist."];
        }

        if (!await _genreRepository.AnyAsync(genre => genre.Id == command.GenreId, cancellationToken))
        {
            errors["genreId"] = [$"Genre '{command.GenreId}' does not exist."];
        }

        if (errors.Count > 0)
        {
            return Result<BookResponse>.Failure(errors);
        }

        var isbn = string.IsNullOrWhiteSpace(command.Isbn) ? null : IsbnValidator.Normalize(command.Isbn);

        if (isbn is not null && await _bookRepository.IsIsbnTakenAsync(isbn, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"A book with ISBN '{isbn}' already exists.");
        }

        var book = new Book
        {
            Id = Guid.CreateVersion7(),
            Title = command.Title.Trim(),
            Isbn = isbn,
            PublishedYear = command.PublishedYear,
            AuthorId = command.AuthorId,
            GenreId = command.GenreId,
        };
        _bookRepository.Add(book);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _bookRepository.GetByIdWithDetailsAsync(book.Id, cancellationToken)
            ?? throw new NotFoundException($"Book '{book.Id}' was not found after creation.");

        return Result<BookResponse>.Success(BookMapping.ToResponse(created));
    }
}
