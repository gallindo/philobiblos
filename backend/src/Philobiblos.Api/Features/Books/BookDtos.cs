using Philobiblos.Api.Domain;

namespace Philobiblos.Api.Features.Books;

public sealed record AuthorSummary(Guid Id, string Name);

public sealed record GenreSummary(Guid Id, string Name);

public sealed record BookResponse(
    Guid Id,
    string Title,
    string? Isbn,
    int? PublishedYear,
    AuthorSummary Author,
    GenreSummary Genre);

public static class BookMapping
{
    public static BookResponse ToResponse(Book book) => new(
        book.Id,
        book.Title,
        book.Isbn,
        book.PublishedYear,
        new AuthorSummary(book.Author.Id, book.Author.Name),
        new GenreSummary(book.Genre.Id, book.Genre.Name));

    public static IQueryable<BookResponse> ProjectToResponse(this IQueryable<Book> books) =>
        books.Select(book => new BookResponse(
            book.Id,
            book.Title,
            book.Isbn,
            book.PublishedYear,
            new AuthorSummary(book.Author.Id, book.Author.Name),
            new GenreSummary(book.Genre.Id, book.Genre.Name)));
}
