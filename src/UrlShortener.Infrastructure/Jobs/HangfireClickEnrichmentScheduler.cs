using Hangfire;
using UrlShortener.Application.Common.Interfaces;

namespace UrlShortener.Infrastructure.Jobs;

// Hangfire serializes the method invocation by interface, so the worker
// resolves IClickEnrichmentJob through DI before executing — keeps the
// service composition uniform between request and background contexts.
public class HangfireClickEnrichmentScheduler(IBackgroundJobClient jobs) : IClickEnrichmentScheduler
{
    public void Schedule(Guid clickId) =>
        jobs.Enqueue<IClickEnrichmentJob>(job => job.RunAsync(clickId, CancellationToken.None));
}
