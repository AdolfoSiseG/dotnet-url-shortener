using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Application.Common.Models;

namespace UrlShortener.Infrastructure.Geolocation;

// Calls ip-api.com (free tier, no key, ~45 req/min/IP).
// HttpClient and resilience pipeline are configured in Infrastructure DI.
public class IpApiGeolocationService(
    HttpClient http,
    ILogger<IpApiGeolocationService> logger) : IIpGeolocationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<GeoLookupResult?> LookupAsync(string ipAddress, CancellationToken ct = default)
    {
        // Skip private/loopback addresses: ip-api would return a failure for
        // them and we'd waste a slot in the rate-limit window.
        if (IsPrivateOrLoopback(ipAddress)) return null;

        try
        {
            using var response = await http.GetAsync(
                $"json/{Uri.EscapeDataString(ipAddress)}?fields=status,country,city",
                ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogDebug("Geo lookup HTTP {Status} for {Ip}", response.StatusCode, ipAddress);
                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<IpApiResponse>(JsonOptions, ct);
            if (body is null || body.Status != "success") return null;

            return new GeoLookupResult(body.Country, body.City);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Resilience pipeline already retried; this is the final failure.
            logger.LogWarning(ex, "Geo lookup failed for {Ip}", ipAddress);
            return null;
        }
    }

    private static bool IsPrivateOrLoopback(string ip)
    {
        if (!IPAddress.TryParse(ip, out var addr)) return true;
        if (IPAddress.IsLoopback(addr)) return true;

        if (addr.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = addr.GetAddressBytes();
            return bytes[0] switch
            {
                10 => true,
                127 => true,
                172 when bytes[1] >= 16 && bytes[1] <= 31 => true,
                192 when bytes[1] == 168 => true,
                _ => false
            };
        }

        // IPv6 link-local and unique-local ranges.
        var ipv6 = addr.ToString();
        return ipv6.StartsWith("fe80:") || ipv6.StartsWith("fc") || ipv6.StartsWith("fd");
    }

    private sealed record IpApiResponse(
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("country")] string? Country,
        [property: JsonPropertyName("city")] string? City);
}
