using UrlShortener.Application.Analytics.Dtos;

namespace UrlShortener.Application.Analytics.Interfaces;

public interface IAnalyticsService
{
    // Returns null when the link does not exist or does not belong to the user.
    Task<LinkStatsDto?> GetLinkStatsAsync(Guid userId, Guid linkId, CancellationToken ct = default);

    Task<OverviewStatsDto> GetOverviewAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<CountryStatDto>> GetByCountryAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<DeviceStatDto>> GetByDeviceAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<TimeBucketStatDto>> GetByTimeAsync(Guid userId, ByTimeQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<ReferrerStatDto>> GetByReferrerAsync(Guid userId, CancellationToken ct = default);
}
