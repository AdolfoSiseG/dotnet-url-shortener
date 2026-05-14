namespace UrlShortener.Application.Analytics.Dtos;

public record OverviewStatsDto(
    int TotalLinks,
    int ActiveLinks,
    int TotalClicks,
    IReadOnlyList<TopLinkDto> TopLinks);
