using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Api.Data;
using Philobiblos.Api.Domain;
using Philobiblos.Api.Features.Genres;
using Philobiblos.Api.Infrastructure;

namespace Philobiblos.UnitTests.Handlers;

public sealed class GenreBusinessRuleTests
{
    private static LibraryDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LibraryDbContext(options);
    }

    [Fact]
    public async Task CreateGenre_throws_conflict_when_name_already_exists_case_insensitive()
    {
        await using var db = CreateInMemoryContext();
        await CreateGenre.Handle(new CreateGenreRequest("Fantasy"), db, default);

        var action = async () => await CreateGenre.Handle(new CreateGenreRequest(" fantasy "), db, default);

        await action.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task UpdateGenre_throws_conflict_when_new_name_matches_another_genre()
    {
        await using var db = CreateInMemoryContext();
        await CreateGenre.Handle(new CreateGenreRequest("Fantasy"), db, default);
        var second = await CreateGenre.Handle(new CreateGenreRequest("Science Fiction"), db, default);
        var secondId = second.Value!.Id;

        var action = async () =>
            await UpdateGenre.Handle(secondId, new UpdateGenreRequest("fantasy"), db, default);

        await action.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task DeleteGenre_throws_conflict_when_genre_is_referenced_by_a_book()
    {
        await using var db = CreateInMemoryContext();
        var genreResponse = await CreateGenre.Handle(new CreateGenreRequest("Fantasy"), db, default);
        var genreId = genreResponse.Value!.Id;
        var authorId = Guid.CreateVersion7();
        db.Authors.Add(new Author { Id = authorId, Name = "Author" });
        db.Books.Add(new Book
        {
            Id = Guid.CreateVersion7(),
            Title = "Book",
            AuthorId = authorId,
            GenreId = genreId
        });
        await db.SaveChangesAsync();

        var action = async () => await DeleteGenre.Handle(genreId, db, default);

        await action.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task DeleteGenre_succeeds_when_genre_has_no_books()
    {
        await using var db = CreateInMemoryContext();
        var genreResponse = await CreateGenre.Handle(new CreateGenreRequest("Fantasy"), db, default);
        var id = genreResponse.Value!.Id;

        await DeleteGenre.Handle(id, db, default);

        (await db.Genres.AnyAsync(genre => genre.Id == id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteGenre_throws_not_found_when_genre_does_not_exist()
    {
        await using var db = CreateInMemoryContext();

        var action = async () => await DeleteGenre.Handle(Guid.CreateVersion7(), db, default);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
