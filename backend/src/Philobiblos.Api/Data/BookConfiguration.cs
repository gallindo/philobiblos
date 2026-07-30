using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Philobiblos.Api.Domain;

namespace Philobiblos.Api.Data;

public sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.HasKey(book => book.Id);

        builder.Property(book => book.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(book => book.Isbn)
            .HasMaxLength(17);

        builder.HasIndex(book => book.Isbn)
            .IsUnique()
            .HasFilter("\"Isbn\" IS NOT NULL");

        builder.HasOne(book => book.Author)
            .WithMany()
            .HasForeignKey(book => book.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(book => book.Genre)
            .WithMany()
            .HasForeignKey(book => book.GenreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
