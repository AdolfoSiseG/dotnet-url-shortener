namespace UrlShortener.Application.Analytics.Dtos;

// Bucket is the truncated start of the period (e.g. 2026-05-13T00:00:00Z
// for a daily bucket). UTC by convention.
public record TimeBucketStatDto(DateTime Bucket, int Clicks);
