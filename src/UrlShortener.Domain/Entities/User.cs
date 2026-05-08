namespace UrlShortener.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<ShortLink> ShortLinks { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
