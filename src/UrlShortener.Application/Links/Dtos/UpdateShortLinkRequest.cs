namespace UrlShortener.Application.Links.Dtos;

// Nullable means "do not change". Clearing a field (e.g. removing the title)
// is not supported in v1.0 because PATCH null is ambiguous between "skip"
// and "clear"; clear semantics would need a JSON Merge Patch envelope.
public record UpdateShortLinkRequest(
    string? Title = null,
    DateTime? ExpiresAt = null,
    bool? IsActive = null);
