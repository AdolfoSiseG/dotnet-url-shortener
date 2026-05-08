using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence.Configurations;

public class ShortLinkConfiguration : IEntityTypeConfiguration<ShortLink>
{
    public void Configure(EntityTypeBuilder<ShortLink> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ShortCode).IsRequired().HasMaxLength(10);
        builder.Property(l => l.OriginalUrl).IsRequired().HasMaxLength(2048);
        builder.Property(l => l.Title).HasMaxLength(256);
        builder.Property(l => l.PasswordHash).HasMaxLength(255);

        // Used on every public redirect, must be unique and indexed.
        builder.HasIndex(l => l.ShortCode).IsUnique();

        // Soft delete: hides DeletedAt != null from every default query.
        // Repositories that need deleted rows (e.g. an admin-only restore
        // feature) must opt in via IgnoreQueryFilters().
        builder.HasQueryFilter(l => l.DeletedAt == null);

        builder
            .HasMany(l => l.Clicks)
            .WithOne(c => c.ShortLink)
            .HasForeignKey(c => c.ShortLinkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
