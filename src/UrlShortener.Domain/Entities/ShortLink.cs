namespace UrlShortener.Domain.Entities;

public class ShortLink
{
    public Guid Id { get; set; }

    // Nullable so anonymous (unregistered) users can create links.
    public Guid? UserId { get; set; }

    public required string ShortCode { get; set; }
    public required string OriginalUrl { get; set; }
    public string? Title { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
    public ICollection<Click> Clicks { get; set; } = [];
}
