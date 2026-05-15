namespace UrlShortener.Application.Links.Dtos;

// HasPassword is exposed instead of the raw hash so clients can render a
// "protected" badge without ever seeing credential material.
public record ShortLinkDto(
    Guid Id,
    string ShortCode,
    string OriginalUrl,
    string? Title,
    DateTime? ExpiresAt,
    bool IsActive,
    bool HasPassword,
    DateTime CreatedAt);
