using FluentAssertions;
using Philobiblos.Application.Authors.Commands;
using Philobiblos.Application.Books.Commands;
using Philobiblos.Application.Genres.Commands;
using Philobiblos.Domain.Exceptions;
using Philobiblos.UnitTests.Common;

namespace Philobiblos.UnitTests.Handlers;

public sealed class AuthorBusinessRuleTests
{
    [Fact]
    public async Task CreateAuthor_throws_conflict_when_name_already_exists_case_insensitive()
    {
        await using var harness = new TestHarness();
        var handler = new CreateAuthorCommandHandler(harness.Authors, harness.UnitOfWork);
        await handler.Handle(new CreateAuthorCommand("Jane Doe", null), default);

        var action = async () => await handler.Handle(new CreateAuthorCommand(" jane doe ", null), default);

        await action.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task UpdateAuthor_throws_conflict_when_new_name_matches_another_author()
    {
        await using var harness = new TestHarness();
        var createHandler = new CreateAuthorCommandHandler(harness.Authors, harness.UnitOfWork);
        var first = await createHandler.Handle(new CreateAuthorCommand("Jane Doe", null), default);
        var second = await createHandler.Handle(new CreateAuthorCommand("John Smith", null), default);

        var updateHandler = new UpdateAuthorCommandHandler(harness.Authors, harness.UnitOfWork);
        var action = async () =>
            await updateHandler.Handle(new UpdateAuthorCommand(second.Id, "JANE DOE", null), default);

        await action.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task DeleteAuthor_throws_conflict_when_author_is_referenced_by_a_book()
    {
        await using var harness = new TestHarness();
        var author = await new CreateAuthorCommandHandler(harness.Authors, harness.UnitOfWork)
            .Handle(new CreateAuthorCommand("Jane Doe", null), default);
        var genre = await new CreateGenreCommandHandler(harness.Genres, harness.UnitOfWork)
            .Handle(new CreateGenreCommand("Genre"), default);
        await new CreateBookCommandHandler(harness.Books, harness.Authors, harness.Genres, harness.UnitOfWork)
            .Handle(new CreateBookCommand("Book", author.Id, genre.Id, null, null), default);

        var deleteHandler = new DeleteAuthorCommandHandler(harness.Authors, harness.UnitOfWork);
        var action = async () => await deleteHandler.Handle(new DeleteAuthorCommand(author.Id), default);

        await action.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task DeleteAuthor_succeeds_when_author_has_no_books()
    {
        await using var harness = new TestHarness();
        var createHandler = new CreateAuthorCommandHandler(harness.Authors, harness.UnitOfWork);
        var author = await createHandler.Handle(new CreateAuthorCommand("Jane Doe", null), default);

        var deleteHandler = new DeleteAuthorCommandHandler(harness.Authors, harness.UnitOfWork);
        await deleteHandler.Handle(new DeleteAuthorCommand(author.Id), default);

        (await harness.Authors.AnyAsync(a => a.Id == author.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAuthor_throws_not_found_when_author_does_not_exist()
    {
        await using var harness = new TestHarness();
        var handler = new DeleteAuthorCommandHandler(harness.Authors, harness.UnitOfWork);

        var action = async () => await handler.Handle(new DeleteAuthorCommand(Guid.CreateVersion7()), default);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
