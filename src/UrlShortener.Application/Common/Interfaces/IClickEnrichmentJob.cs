namespace UrlShortener.Application.Common.Interfaces;

// Background job that fills geo/device/browser/IsBot on a click row.
// Dispatched by the Hangfire worker after the redirect endpoint enqueues it.
public interface IClickEnrichmentJob
{
    Task RunAsync(Guid clickId, CancellationToken ct = default);
}
