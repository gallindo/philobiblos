using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Philobiblos.Api.Domain;

namespace Philobiblos.Api.Data;

public sealed class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder.HasKey(author => author.Id);

        builder.Property(author => author.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(author => author.Bio)
            .HasMaxLength(2000);

        // Case-insensitive name uniqueness is enforced by a unique index on lower("Name"),
        // created with raw SQL in the initial migration (see Data/Migrations).
    }
}
