using UrlShortener.Application.Common.Models;

namespace UrlShortener.Application.Common.Interfaces;

public interface IIpGeolocationService
{
    // Returns null when the address is private/loopback, when the upstream
    // service has nothing useful, or when the request fails. Callers should
    // treat null as "no geo info available" and proceed.
    Task<GeoLookupResult?> LookupAsync(string ipAddress, CancellationToken ct = default);
}
