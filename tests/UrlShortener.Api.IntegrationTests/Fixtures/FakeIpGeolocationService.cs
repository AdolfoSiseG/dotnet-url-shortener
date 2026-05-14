using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Application.Common.Models;

namespace UrlShortener.Api.IntegrationTests.Fixtures;

// Always returns null — the click enrichment path tolerates a missing geo
// result and tests should never make real network calls to ip-api.com.
public class FakeIpGeolocationService : IIpGeolocationService
{
    public Task<GeoLookupResult?> LookupAsync(string ipAddress, CancellationToken ct = default) =>
        Task.FromResult<GeoLookupResult?>(null);
}
