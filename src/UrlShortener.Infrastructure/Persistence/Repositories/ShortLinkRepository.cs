using Microsoft.EntityFrameworkCore;
using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Application.Links.Dtos;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence.Repositories;

public class ShortLinkRepository(AppDbContext db) : IShortLinkRepository
{
    public Task<bool> ExistsByShortCodeAsync(string shortCode, CancellationToken ct = default) =>
        db.ShortLinks.AnyAsync(l => l.ShortCode == shortCode, ct);

    public Task<ShortLink?> FindByIdAsync(Guid userId, Guid id, CancellationToken ct = default) =>
        db.ShortLinks.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId, ct);

    public Task<ShortLink?> FindByShortCodeAsync(string shortCode, CancellationToken ct = default) =>
        db.ShortLinks.FirstOrDefaultAsync(l => l.ShortCode == shortCode, ct);

    public async Task<(IReadOnlyList<ShortLink> Items, int Total)> ListAsync(
        Guid userId,
        ListShortLinksQuery query,
        CancellationToken ct = default)
    {
        var queryable = db.ShortLinks.Where(l => l.UserId == userId);

        queryable = ApplyStatusFilter(queryable, query.Status);

        if (!string.IsNullOrEmpty(query.Search))
        {
            // EF.Functions.ILike maps to Postgres' ILIKE for case-insensitive
            // matching. % is the SQL wildcard for any sequence of characters.
            var pattern = $"%{query.Search}%";
            queryable = queryable.Where(l =>
                EF.Functions.ILike(l.OriginalUrl, pattern)
                || (l.Title != null && EF.Functions.ILike(l.Title, pattern)));
        }

        var total = await queryable.CountAsync(ct);

        var items = await queryable
            .OrderByDescending(l => l.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(ShortLink link, CancellationToken ct = default)
    {
        await db.ShortLinks.AddAsync(link, ct);
    }

    private static IQueryable<ShortLink> ApplyStatusFilter(IQueryable<ShortLink> source, string? status)
    {
        var now = DateTime.UtcNow;
        return status switch
        {
            "active" => source.Where(l => l.IsActive && (l.ExpiresAt == null || l.ExpiresAt > now)),
            "expired" => source.Where(l => !l.IsActive || (l.ExpiresAt != null && l.ExpiresAt <= now)),
            _ => source
        };
    }
}
