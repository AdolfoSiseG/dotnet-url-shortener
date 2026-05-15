using UrlShortener.Application.Links.Dtos;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Common.Interfaces;

public interface IShortLinkRepository
{
    Task<bool> ExistsByShortCodeAsync(string shortCode, CancellationToken ct = default);

    // Scoped by userId so callers cannot accidentally fetch another user's
    // link. The repository is the right place for this guard because it
    // owns the query.
    Task<ShortLink?> FindByIdAsync(Guid userId, Guid id, CancellationToken ct = default);

    // Public lookup used by the redirect endpoint. The soft-delete global
    // query filter on ShortLinkConfiguration auto-hides deleted rows here.
    Task<ShortLink?> FindByShortCodeAsync(string shortCode, CancellationToken ct = default);

    Task<(IReadOnlyList<ShortLink> Items, int Total)> ListAsync(
        Guid userId,
        ListShortLinksQuery query,
        CancellationToken ct = default);

    Task AddAsync(ShortLink link, CancellationToken ct = default);
}
