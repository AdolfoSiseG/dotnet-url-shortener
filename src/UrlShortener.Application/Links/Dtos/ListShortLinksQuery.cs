namespace UrlShortener.Application.Links.Dtos;

// Status:
//   "active"  -> IsActive = true and not expired
//   "expired" -> IsActive = false or past ExpiresAt
//   null      -> no status filter (default)
public record ListShortLinksQuery(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    string? Search = null);
