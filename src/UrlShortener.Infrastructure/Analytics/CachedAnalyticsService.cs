using Microsoft.Extensions.Caching.Memory;
using UrlShortener.Application.Analytics.Dtos;
using UrlShortener.Application.Analytics.Interfaces;

namespace UrlShortener.Infrastructure.Analytics;

// Decorator over AnalyticsService that caches only the overview endpoint —
// the only stats query whose result is small, accessed often, and stale-
// tolerant. Per-link, by-country and by-time queries are not cached because
// either their parameter space is too large (by-time) or their data changes
// more often than the 15-minute TTL would tolerate.
//
// IMPORTANT: IMemoryCache is per-process. When the API moves to multiple
// replicas (week 12 or later), this must be swapped for a distributed cache
// such as Redis to avoid divergent reads between instances.
public class CachedAnalyticsService(
    AnalyticsService inner,
    IMemoryCache cache) : IAnalyticsService
{
    private static readonly TimeSpan OverviewTtl = TimeSpan.FromMinutes(15);

    public Task<LinkStatsDto?> GetLinkStatsAsync(Guid userId, Guid linkId, CancellationToken ct = default) =>
        inner.GetLinkStatsAsync(userId, linkId, ct);

    public async Task<OverviewStatsDto> GetOverviewAsync(Guid userId, CancellationToken ct = default)
    {
        var cacheKey = $"analytics:overview:{userId}";
        if (cache.TryGetValue<OverviewStatsDto>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var result = await inner.GetOverviewAsync(userId, ct);
        cache.Set(cacheKey, result, OverviewTtl);
        return result;
    }

    public Task<IReadOnlyList<CountryStatDto>> GetByCountryAsync(Guid userId, CancellationToken ct = default) =>
        inner.GetByCountryAsync(userId, ct);

    public Task<IReadOnlyList<DeviceStatDto>> GetByDeviceAsync(Guid userId, CancellationToken ct = default) =>
        inner.GetByDeviceAsync(userId, ct);

    public Task<IReadOnlyList<TimeBucketStatDto>> GetByTimeAsync(Guid userId, ByTimeQuery query, CancellationToken ct = default) =>
        inner.GetByTimeAsync(userId, query, ct);

    public Task<IReadOnlyList<ReferrerStatDto>> GetByReferrerAsync(Guid userId, CancellationToken ct = default) =>
        inner.GetByReferrerAsync(userId, ct);
}
