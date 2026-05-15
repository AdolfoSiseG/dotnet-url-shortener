namespace UrlShortener.Application.Analytics.Dtos;

public record TopLinkDto(Guid Id, string ShortCode, string OriginalUrl, int Clicks);
