using Microsoft.EntityFrameworkCore;
using UrlShortener.Application.Analytics.Dtos;
using UrlShortener.Application.Analytics.Interfaces;
using UrlShortener.Infrastructure.Persistence;

namespace UrlShortener.Infrastructure.Analytics;

// Lives in Infrastructure (not Application) because every method is a
// translation from query parameters into a SQL aggregation, and pushing it
// through a repository would only mean a one-method-per-query interface.
// The global query filter on ShortLink (DeletedAt == null) and the matching
// filter on Click both apply here, so soft-deleted links and their clicks
// stay out of the numbers.
public class AnalyticsService(AppDbContext db) : IAnalyticsService
{
    private const int TopLinksLimit = 5;
    private const int CountryLimit = 20;
    private const int ReferrerLimit = 20;

    public async Task<LinkStatsDto?> GetLinkStatsAsync(Guid userId, Guid linkId, CancellationToken ct = default)
    {
        var link = await db.ShortLinks
            .Where(l => l.UserId == userId && l.Id == linkId)
            .Select(l => new { l.Id, l.ShortCode })
            .FirstOrDefaultAsync(ct);

        if (link is null) return null;

        var linkClicks = db.Clicks.Where(c => c.ShortLinkId == linkId);

        var totalClicks = await linkClicks.CountAsync(ct);
        var uniqueIps = await linkClicks.Select(c => c.IpAddress).Distinct().CountAsync(ct);
        var lastClickAt = await linkClicks
            .OrderByDescending(c => c.ClickedAt)
            .Select(c => (DateTime?)c.ClickedAt)
            .FirstOrDefaultAsync(ct);

        var byCountry = (await linkClicks
            .Where(c => c.Country != null)
            .GroupBy(c => c.Country!)
            .Select(g => new { Country = g.Key, Clicks = g.Count() })
            .OrderByDescending(x => x.Clicks)
            .Take(CountryLimit)
            .ToListAsync(ct))
            .Select(x => new CountryStatDto(x.Country, x.Clicks))
            .ToList();

        var byDevice = (await linkClicks
            .Where(c => c.Device != null)
            .GroupBy(c => c.Device!)
            .Select(g => new { Device = g.Key, Clicks = g.Count() })
            .OrderByDescending(x => x.Clicks)
            .ToListAsync(ct))
            .Select(x => new DeviceStatDto(x.Device, x.Clicks))
            .ToList();

        return new LinkStatsDto(link.Id, link.ShortCode, totalClicks, uniqueIps, lastClickAt, byCountry, byDevice);
    }

    public async Task<OverviewStatsDto> GetOverviewAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var totalLinks = await db.ShortLinks.CountAsync(l => l.UserId == userId, ct);

        var activeLinks = await db.ShortLinks
            .CountAsync(l => l.UserId == userId
                && l.IsActive
                && (l.ExpiresAt == null || l.ExpiresAt > now), ct);

        var totalClicks = await db.Clicks.CountAsync(c => c.ShortLink.UserId == userId, ct);

        // Order/Take must happen before the projection: EF cannot translate
        // an OrderBy over a constructor argument of a freshly built DTO.
        var topLinks = await db.ShortLinks
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.Clicks.Count())
            .Take(TopLinksLimit)
            .Select(l => new TopLinkDto(l.Id, l.ShortCode, l.OriginalUrl, l.Clicks.Count()))
            .ToListAsync(ct);

        return new OverviewStatsDto(totalLinks, activeLinks, totalClicks, topLinks);
    }

    // Aggregations project to an anonymous row first because EF Core 10
    // cannot translate an OrderBy/Take whose key is a constructor argument
    // of a record DTO. The final projection happens client-side after the
    // small bounded result set has come back.
    public async Task<IReadOnlyList<CountryStatDto>> GetByCountryAsync(Guid userId, CancellationToken ct = default) =>
        (await db.Clicks
            .Where(c => c.ShortLink.UserId == userId && c.Country != null)
            .GroupBy(c => c.Country!)
            .Select(g => new { Country = g.Key, Clicks = g.Count() })
            .OrderByDescending(x => x.Clicks)
            .Take(CountryLimit)
            .ToListAsync(ct))
            .Select(x => new CountryStatDto(x.Country, x.Clicks))
            .ToList();

    public async Task<IReadOnlyList<DeviceStatDto>> GetByDeviceAsync(Guid userId, CancellationToken ct = default) =>
        (await db.Clicks
            .Where(c => c.ShortLink.UserId == userId && c.Device != null)
            .GroupBy(c => c.Device!)
            .Select(g => new { Device = g.Key, Clicks = g.Count() })
            .OrderByDescending(x => x.Clicks)
            .ToListAsync(ct))
            .Select(x => new DeviceStatDto(x.Device, x.Clicks))
            .ToList();

    // Expected query plan on Postgres 17 (verify locally with EXPLAIN ANALYZE):
    //
    //   Sort
    //     Sort Key: date_trunc('day', c."ClickedAt")
    //     -> HashAggregate
    //          Group Key: date_trunc('day', c."ClickedAt")
    //          -> Nested Loop
    //               -> Index Scan on "IX_ShortLinks_UserId"
    //                    Index Cond: ("UserId" = $1)
    //               -> Index Scan on "IX_Clicks_ShortLinkId_ClickedAt"
    //                    Index Cond: ("ShortLinkId" = sl."Id"
    //                                 AND "ClickedAt" >= $2
    //                                 AND "ClickedAt" < $3)
    //
    // The composite index satisfies both the join and the time-range filter
    // in a single scan — see brief sec. 6 for the index design rationale.
    //
    // Raw SQL because Npgsql 10's EF.Functions does not expose date_trunc.
    // The validator restricts granularity to "day"|"week"|"month" so the
    // interpolated value cannot be attacker-controlled. The other values
    // are bound as parameters by SqlQuery.
    public async Task<IReadOnlyList<TimeBucketStatDto>> GetByTimeAsync(
        Guid userId, ByTimeQuery query, CancellationToken ct = default)
    {
        var granularity = query.Granularity.ToLowerInvariant();
        var from = DateTime.SpecifyKind(query.From, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(query.To, DateTimeKind.Utc);

        return await db.Database
            .SqlQuery<TimeBucketStatDto>($"""
                SELECT date_trunc({granularity}, c."ClickedAt") AS "Bucket",
                       COUNT(*)::int AS "Clicks"
                FROM "Clicks" c
                INNER JOIN "ShortLinks" sl ON c."ShortLinkId" = sl."Id"
                WHERE sl."UserId" = {userId}
                  AND sl."DeletedAt" IS NULL
                  AND c."ClickedAt" >= {from}
                  AND c."ClickedAt" < {to}
                GROUP BY 1
                ORDER BY 1
                """)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ReferrerStatDto>> GetByReferrerAsync(Guid userId, CancellationToken ct = default) =>
        (await db.Clicks
            .Where(c => c.ShortLink.UserId == userId && c.Referrer != null)
            .GroupBy(c => c.Referrer!)
            .Select(g => new { Referrer = g.Key, Clicks = g.Count() })
            .OrderByDescending(x => x.Clicks)
            .Take(ReferrerLimit)
            .ToListAsync(ct))
            .Select(x => new ReferrerStatDto(x.Referrer, x.Clicks))
            .ToList();
}
