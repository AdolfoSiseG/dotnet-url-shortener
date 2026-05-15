namespace UrlShortener.Application.Common.Interfaces;

// Coordinates persistence across repositories. Keeps SaveChangesAsync out
// of individual repos so they remain focused on a single aggregate, and
// lets a service group multiple changes into one transaction.
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
