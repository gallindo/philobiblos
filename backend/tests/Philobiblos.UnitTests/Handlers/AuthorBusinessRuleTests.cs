using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Api.Data;
using Philobiblos.Api.Domain;
using Philobiblos.Api.Features.Authors;
using Philobiblos.Api.Infrastructure;

namespace Philobiblos.UnitTests.Handlers;

public sealed class AuthorBusinessRuleTests
{
    private static LibraryDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LibraryDbContext(options);
    }

    [Fact]
    public async Task CreateAuthor_throws_conflict_when_name_already_exists_case_insensitive()
    {
        await using var db = CreateInMemoryContext();
        await CreateAuthor.Handle(new CreateAuthorRequest("Jane Doe", null), db, default);

        var action = async () => await CreateAuthor.Handle(new CreateAuthorRequest(" jane doe ", null), db, default);

        await action.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task UpdateAuthor_throws_conflict_when_new_name_matches_another_author()
    {
        await using var db = CreateInMemoryContext();
        var first =         await CreateAuthor.Handle(new CreateAuthorRequest("Jane Doe", null), db, default);
        var second = await CreateAuthor.Handle(new CreateAuthorRequest("John Smith", null), db, default);
        var secondId = second.Value!.Id;

        var action = async () =>
            await UpdateAuthor.Handle(secondId, new UpdateAuthorRequest("JANE DOE", null), db, default);

        await action.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task DeleteAuthor_throws_conflict_when_author_is_referenced_by_a_book()
    {
        await using var db = CreateInMemoryContext();
        var authorResponse = await CreateAuthor.Handle(new CreateAuthorRequest("Jane Doe", null), db, default);
        var authorId = authorResponse.Value!.Id;
        var genreId = Guid.CreateVersion7();
        db.Genres.Add(new Genre { Id = genreId, Name = "Genre" });
        db.Books.Add(new Book
        {
            Id = Guid.CreateVersion7(),
            Title = "Book",
            AuthorId = authorId,
            GenreId = genreId
        });
        await db.SaveChangesAsync();

        var action = async () => await DeleteAuthor.Handle(authorId, db, default);

        await action.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task DeleteAuthor_succeeds_when_author_has_no_books()
    {
        await using var db = CreateInMemoryContext();
        var authorResponse = await CreateAuthor.Handle(new CreateAuthorRequest("Jane Doe", null), db, default);
        var id = authorResponse.Value!.Id;

        await DeleteAuthor.Handle(id, db, default);

        (await db.Authors.AnyAsync(author => author.Id == id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAuthor_throws_not_found_when_author_does_not_exist()
    {
        await using var db = CreateInMemoryContext();

        var action = async () => await DeleteAuthor.Handle(Guid.CreateVersion7(), db, default);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
