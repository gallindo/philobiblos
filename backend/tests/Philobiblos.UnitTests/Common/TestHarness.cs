using Microsoft.EntityFrameworkCore;
using Philobiblos.Domain.Repositories;
using Philobiblos.Infrastructure.Data;
using Philobiblos.Infrastructure.Repositories;

namespace Philobiblos.UnitTests.Common;

public sealed class TestHarness : IDisposable, IAsyncDisposable
{
    public LibraryDbContext Context { get; }
    public IAuthorRepository Authors { get; }
    public IGenreRepository Genres { get; }
    public IBookRepository Books { get; }
    public IUnitOfWork UnitOfWork { get; }

    public TestHarness()
    {
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        Context = new LibraryDbContext(options);
        Authors = new AuthorRepository(Context);
        Genres = new GenreRepository(Context);
        Books = new BookRepository(Context);
        UnitOfWork = Context;
    }

    public void Dispose()
    {
        Context.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return Context.DisposeAsync();
    }
}
