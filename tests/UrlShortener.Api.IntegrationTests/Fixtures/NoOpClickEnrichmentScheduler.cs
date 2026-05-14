using UrlShortener.Application.Common.Interfaces;

namespace UrlShortener.Api.IntegrationTests.Fixtures;

// Replaces the Hangfire-backed scheduler when Hangfire is disabled in tests.
// The redirect path fires-and-forgets the click id; tests assert on the raw
// Click row (already inserted synchronously) rather than on the enrichment
// outcome.
public class NoOpClickEnrichmentScheduler : IClickEnrichmentScheduler
{
    public void Schedule(Guid clickId) { }
}
