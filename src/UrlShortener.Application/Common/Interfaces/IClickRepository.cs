using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Common.Interfaces;

public interface IClickRepository
{
    Task AddAsync(Click click, CancellationToken ct = default);

    // Used by the enrichment job to load a click for in-place updates.
    Task<Click?> FindByIdAsync(Guid id, CancellationToken ct = default);
}
