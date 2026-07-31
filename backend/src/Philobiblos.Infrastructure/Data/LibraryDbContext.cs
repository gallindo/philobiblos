using Microsoft.EntityFrameworkCore;
using Philobiblos.Domain.Entities;
using Philobiblos.Domain.Repositories;

namespace Philobiblos.Infrastructure.Data;

public sealed class LibraryDbContext(DbContextOptions<LibraryDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Genre> Genres => Set<Genre>();

    public DbSet<Author> Authors => Set<Author>();

    public DbSet<Book> Books => Set<Book>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);
    }
}
