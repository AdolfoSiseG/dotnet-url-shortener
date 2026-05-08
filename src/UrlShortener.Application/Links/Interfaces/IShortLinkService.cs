using UrlShortener.Application.Common.Models;
using UrlShortener.Application.Links.Dtos;

namespace UrlShortener.Application.Links.Interfaces;

public interface IShortLinkService
{
    Task<ShortLinkDto> CreateAsync(Guid userId, CreateShortLinkRequest request, CancellationToken ct = default);
    Task<PaginatedResult<ShortLinkDto>> ListAsync(Guid userId, ListShortLinksQuery query, CancellationToken ct = default);
    Task<ShortLinkDto?> GetAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<ShortLinkDto?> UpdateAsync(Guid userId, Guid id, UpdateShortLinkRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default);
}
