using UrlShortener.Application.Links.Dtos;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Links.Mapping;

internal static class ShortLinkMappings
{
    public static ShortLinkDto ToDto(this ShortLink link) =>
        new(
            link.Id,
            link.ShortCode,
            link.OriginalUrl,
            link.Title,
            link.ExpiresAt,
            link.IsActive,
            link.PasswordHash is not null,
            link.CreatedAt);
}
