using FluentAssertions;
using Philobiblos.Application.Authors.Commands;
using Philobiblos.Application.Books.Commands;
using Philobiblos.Application.Genres.Commands;
using Philobiblos.Domain.Exceptions;
using Philobiblos.UnitTests.Common;

namespace Philobiblos.UnitTests.Handlers;

public sealed class GenreBusinessRuleTests
{
    [Fact]
    public async Task CreateGenre_throws_conflict_when_name_already_exists_case_insensitive()
    {
        await using var harness = new TestHarness();
        var handler = new CreateGenreCommandHandler(harness.Genres, harness.UnitOfWork);
        await handler.Handle(new CreateGenreCommand("Fantasy"), default);

        var action = async () => await handler.Handle(new CreateGenreCommand(" fantasy "), default);

        await action.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task UpdateGenre_throws_conflict_when_new_name_matches_another_genre()
    {
        await using var harness = new TestHarness();
        var createHandler = new CreateGenreCommandHandler(harness.Genres, harness.UnitOfWork);
        await createHandler.Handle(new CreateGenreCommand("Fantasy"), default);
        var second = await createHandler.Handle(new CreateGenreCommand("Science Fiction"), default);

        var updateHandler = new UpdateGenreCommandHandler(harness.Genres, harness.UnitOfWork);
        var action = async () =>
            await updateHandler.Handle(new UpdateGenreCommand(second.Id, "fantasy"), default);

        await action.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task DeleteGenre_throws_conflict_when_genre_is_referenced_by_a_book()
    {
        await using var harness = new TestHarness();
        var author = await new CreateAuthorCommandHandler(harness.Authors, harness.UnitOfWork)
            .Handle(new CreateAuthorCommand("Author", null), default);
        var genre = await new CreateGenreCommandHandler(harness.Genres, harness.UnitOfWork)
            .Handle(new CreateGenreCommand("Genre"), default);
        await new CreateBookCommandHandler(harness.Books, harness.Authors, harness.Genres, harness.UnitOfWork)
            .Handle(new CreateBookCommand("Book", author.Id, genre.Id, null, null), default);

        var deleteHandler = new DeleteGenreCommandHandler(harness.Genres, harness.UnitOfWork);
        var action = async () => await deleteHandler.Handle(new DeleteGenreCommand(genre.Id), default);

        await action.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task DeleteGenre_succeeds_when_genre_has_no_books()
    {
        await using var harness = new TestHarness();
        var createHandler = new CreateGenreCommandHandler(harness.Genres, harness.UnitOfWork);
        var genre = await createHandler.Handle(new CreateGenreCommand("Fantasy"), default);

        var deleteHandler = new DeleteGenreCommandHandler(harness.Genres, harness.UnitOfWork);
        await deleteHandler.Handle(new DeleteGenreCommand(genre.Id), default);

        (await harness.Genres.AnyAsync(g => g.Id == genre.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteGenre_throws_not_found_when_genre_does_not_exist()
    {
        await using var harness = new TestHarness();
        var handler = new DeleteGenreCommandHandler(harness.Genres, harness.UnitOfWork);

        var action = async () => await handler.Handle(new DeleteGenreCommand(Guid.CreateVersion7()), default);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
