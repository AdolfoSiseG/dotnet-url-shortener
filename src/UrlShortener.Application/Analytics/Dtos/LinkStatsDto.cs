namespace UrlShortener.Application.Analytics.Dtos;

// Aggregated view of a single short link. Bundles the breakdowns a UI
// dashboard typically renders together so the client makes one request
// instead of four.
public record LinkStatsDto(
    Guid Id,
    string ShortCode,
    int TotalClicks,
    int UniqueIps,
    DateTime? LastClickAt,
    IReadOnlyList<CountryStatDto> ByCountry,
    IReadOnlyList<DeviceStatDto> ByDevice);
