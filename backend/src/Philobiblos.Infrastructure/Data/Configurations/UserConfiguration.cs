using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Philobiblos.Domain.Entities;

namespace Philobiblos.Infrastructure.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);

        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(user => user.DisplayName)
            .HasMaxLength(256);

        builder.Property(user => user.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(user => user.ProviderSubject)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(256);

        builder.Property(user => user.Role)
            .IsRequired();

        builder.Property(user => user.CreatedAt)
            .IsRequired();

        builder.HasIndex(user => new { user.Provider, user.ProviderSubject })
            .IsUnique();

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email_Lower");
    }
}
