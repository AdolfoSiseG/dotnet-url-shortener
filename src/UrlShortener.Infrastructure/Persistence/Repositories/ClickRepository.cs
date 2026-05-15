using Microsoft.EntityFrameworkCore;
using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence.Repositories;

public class ClickRepository(AppDbContext db) : IClickRepository
{
    public async Task AddAsync(Click click, CancellationToken ct = default)
    {
        await db.Clicks.AddAsync(click, ct);
    }

    public Task<Click?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Clicks.FirstOrDefaultAsync(c => c.Id == id, ct);
}
