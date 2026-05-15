using Microsoft.Extensions.Logging;
using UrlShortener.Application.Common.Interfaces;

namespace UrlShortener.Infrastructure.Jobs;

// Hangfire resolves this through DI and invokes RunAsync. Failures bubble up
// so Hangfire's own retry policy applies — see HangfireServer configuration
// in Program.cs / DependencyInjection.cs.
public class ClickEnrichmentJob(
    IClickRepository clicks,
    IUserAgentParser uaParser,
    IIpGeolocationService geo,
    IUnitOfWork unitOfWork,
    ILogger<ClickEnrichmentJob> logger) : IClickEnrichmentJob
{
    public async Task RunAsync(Guid clickId, CancellationToken ct = default)
    {
        var click = await clicks.FindByIdAsync(clickId, ct);
        if (click is null)
        {
            // The click was deleted (cascade from soft-deleted parent, manual
            // cleanup) between enqueue and execution. Drop the job quietly.
            logger.LogDebug("Click {ClickId} not found for enrichment", clickId);
            return;
        }

        var ua = uaParser.Parse(click.UserAgent);
        click.Browser = ua.Browser;
        click.Os = ua.Os;
        click.Device = ua.Device;
        click.IsBot = ua.IsBot;

        // Bots typically use datacenter IPs; geo for them is noisy and the
        // free tier rate limit is finite. Skip the lookup for known bots.
        if (!ua.IsBot)
        {
            var geoResult = await geo.LookupAsync(click.IpAddress, ct);
            if (geoResult is not null)
            {
                click.Country = geoResult.Country;
                click.City = geoResult.City;
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
