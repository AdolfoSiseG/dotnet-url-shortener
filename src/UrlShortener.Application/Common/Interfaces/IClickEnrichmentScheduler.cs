namespace UrlShortener.Application.Common.Interfaces;

// Thin abstraction so the Application layer can request a click enrichment
// without referencing Hangfire types directly. Implemented in Infrastructure.
public interface IClickEnrichmentScheduler
{
    void Schedule(Guid clickId);
}
