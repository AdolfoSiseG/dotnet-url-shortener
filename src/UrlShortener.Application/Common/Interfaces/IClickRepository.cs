using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Common.Interfaces;

public interface IClickRepository
{
    Task AddAsync(Click click, CancellationToken ct = default);
}
