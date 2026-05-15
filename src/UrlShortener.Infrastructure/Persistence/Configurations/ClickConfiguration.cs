using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence.Configurations;

public class ClickConfiguration : IEntityTypeConfiguration<Click>
{
    public void Configure(EntityTypeBuilder<Click> builder)
    {
        builder.HasKey(c => c.Id);

        // 45 chars covers the longest IPv6 representation (with embedded IPv4).
        builder.Property(c => c.IpAddress).IsRequired().HasMaxLength(45);
        builder.Property(c => c.UserAgent).IsRequired().HasMaxLength(512);
        builder.Property(c => c.Country).HasMaxLength(64);
        builder.Property(c => c.City).HasMaxLength(128);
        builder.Property(c => c.Browser).HasMaxLength(64);
        builder.Property(c => c.Os).HasMaxLength(64);
        builder.Property(c => c.Device).HasMaxLength(16);
        builder.Property(c => c.Referrer).HasMaxLength(2048);

        // Composite index for analytics queries that filter by link first
        // and then aggregate or order by time. Column order matters here.
        builder.HasIndex(c => new { c.ShortLinkId, c.ClickedAt });

        // Standalone index for global time-series aggregations.
        builder.HasIndex(c => c.ClickedAt);

        // Matches the soft-delete filter on the parent ShortLink so a click
        // for a deleted link is hidden from default queries too. Use
        // IgnoreQueryFilters() in places that need historical analytics
        // (e.g. an admin restore-and-recover screen).
        builder.HasQueryFilter(c => c.ShortLink.DeletedAt == null);
    }
}
