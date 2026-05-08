namespace UrlShortener.Application.Links.Dtos;

public record CreateShortLinkRequest(
    string OriginalUrl,
    string? Title = null,
    string? CustomSlug = null,
    DateTime? ExpiresAt = null,
    string? Password = null);
