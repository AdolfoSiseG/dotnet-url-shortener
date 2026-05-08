using UrlShortener.Application.Auth.Interfaces;
using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Application.Common.Models;
using UrlShortener.Application.Links.Dtos;
using UrlShortener.Application.Links.Interfaces;
using UrlShortener.Application.Links.Mapping;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain.Exceptions;

namespace UrlShortener.Application.Links.Services;

public class ShortLinkService(
    IShortLinkRepository links,
    IShortCodeGenerator codeGenerator,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork) : IShortLinkService
{
    private const int CollisionRetryAttempts = 5;

    public async Task<ShortLinkDto> CreateAsync(Guid userId, CreateShortLinkRequest request, CancellationToken ct = default)
    {
        var shortCode = string.IsNullOrEmpty(request.CustomSlug)
            ? await GenerateUniqueShortCodeAsync(ct)
            : await EnsureCustomSlugIsAvailableAsync(request.CustomSlug, ct);

        var link = new ShortLink
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ShortCode = shortCode,
            OriginalUrl = request.OriginalUrl,
            Title = request.Title,
            ExpiresAt = request.ExpiresAt,
            IsActive = true,
            PasswordHash = string.IsNullOrEmpty(request.Password)
                ? null
                : passwordHasher.Hash(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        await links.AddAsync(link, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return link.ToDto();
    }

    public async Task<PaginatedResult<ShortLinkDto>> ListAsync(Guid userId, ListShortLinksQuery query, CancellationToken ct = default)
    {
        var (items, total) = await links.ListAsync(userId, query, ct);
        return new PaginatedResult<ShortLinkDto>(
            items.Select(l => l.ToDto()).ToList(),
            query.Page,
            query.PageSize,
            total);
    }

    public async Task<ShortLinkDto?> GetAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var link = await links.FindByIdAsync(userId, id, ct);
        return link?.ToDto();
    }

    public async Task<ShortLinkDto?> UpdateAsync(Guid userId, Guid id, UpdateShortLinkRequest request, CancellationToken ct = default)
    {
        var link = await links.FindByIdAsync(userId, id, ct);
        if (link is null) return null;

        // Nullable fields on the request mean "do not change". A clear-the-
        // value semantic is intentionally not supported in v1.0.
        if (request.Title is not null) link.Title = request.Title;
        if (request.ExpiresAt is not null) link.ExpiresAt = request.ExpiresAt;
        if (request.IsActive is not null) link.IsActive = request.IsActive.Value;

        await unitOfWork.SaveChangesAsync(ct);
        return link.ToDto();
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var link = await links.FindByIdAsync(userId, id, ct);
        if (link is null) return false;

        // Soft delete: ShortLinkConfiguration's global query filter hides
        // DeletedAt != null from every default query, so the link disappears
        // from list/get without a hard DELETE.
        link.DeletedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    private async Task<string> GenerateUniqueShortCodeAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < CollisionRetryAttempts; attempt++)
        {
            var code = codeGenerator.Generate();
            if (!await links.ExistsByShortCodeAsync(code, ct))
            {
                return code;
            }
        }
        throw new ShortCodeGenerationException();
    }

    private async Task<string> EnsureCustomSlugIsAvailableAsync(string slug, CancellationToken ct)
    {
        if (await links.ExistsByShortCodeAsync(slug, ct))
        {
            throw new ShortCodeAlreadyTakenException(slug);
        }
        return slug;
    }
}
