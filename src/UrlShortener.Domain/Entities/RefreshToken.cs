namespace UrlShortener.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    // We store SHA-256 of the raw token, never the token itself, so that
    // a database leak is not enough to impersonate a user.
    public required string TokenHash { get; set; }

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    // Hash of the token issued when this one was rotated. Lets us trace a
    // chain of rotations to detect re-use of an old token in a future
    // hardened build (stolen-token detection).
    public string? ReplacedByTokenHash { get; set; }

    public User User { get; set; } = null!;

    public bool IsRevoked => RevokedAt is not null;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}
