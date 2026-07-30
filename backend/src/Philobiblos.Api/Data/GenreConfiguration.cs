using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Philobiblos.Api.Domain;

namespace Philobiblos.Api.Data;

public sealed class GenreConfiguration : IEntityTypeConfiguration<Genre>
{
    public void Configure(EntityTypeBuilder<Genre> builder)
    {
        builder.HasKey(genre => genre.Id);

        builder.Property(genre => genre.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Case-insensitive name uniqueness is enforced by a unique index on lower("Name").
        // EF Core cannot express function-based indexes, so the initial migration creates
        // it with raw SQL (see Data/Migrations).
    }
}
