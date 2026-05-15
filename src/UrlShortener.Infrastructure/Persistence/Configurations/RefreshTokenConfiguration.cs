using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(r => r.Id);

        // SHA-256 hex is 64 chars; 128 leaves slack for switching to SHA-512
        // or base64 without revisiting the schema.
        builder.Property(r => r.TokenHash).IsRequired().HasMaxLength(128);
        builder.Property(r => r.ReplacedByTokenHash).HasMaxLength(128);

        // Looked up by hash on every refresh; must be unique to prevent
        // accidental collisions across users.
        builder.HasIndex(r => r.TokenHash).IsUnique();

        // Computed properties on the entity; not mapped to columns.
        builder.Ignore(r => r.IsRevoked);
        builder.Ignore(r => r.IsExpired);
        builder.Ignore(r => r.IsActive);
    }
}
