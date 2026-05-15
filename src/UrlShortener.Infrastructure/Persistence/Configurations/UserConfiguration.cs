using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.PasswordHash).IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();

        builder
            .HasMany(u => u.ShortLinks)
            .WithOne(l => l.User)
            .HasForeignKey(l => l.UserId)
            // Anonymous-link policy: deleting a user does not destroy their
            // links; the FK is nulled and the links live on as anonymous.
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasMany(u => u.RefreshTokens)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            // Refresh tokens are user-owned and meaningless without the user.
            .OnDelete(DeleteBehavior.Cascade);
    }
}
