using UrlShortener.Application.Auth.Interfaces;
using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Application.Redirects.Interfaces;
using UrlShortener.Application.Redirects.Models;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Redirects.Services;

public class RedirectService(
    IShortLinkRepository links,
    IClickRepository clicks,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork) : IRedirectService
{
    public async Task<RedirectResult> ResolveAsync(string shortCode, ClickContext context, CancellationToken ct = default)
    {
        var link = await links.FindByShortCodeAsync(shortCode, ct);

        if (link is null) return new RedirectNotFound();
        if (!IsActive(link)) return new RedirectGone();
        if (link.PasswordHash is not null) return new RedirectPasswordRequired();

        await RecordClickAsync(link.Id, context, ct);
        return new RedirectFound(link.OriginalUrl);
    }

    public async Task<RedirectResult> UnlockAsync(string shortCode, string password, ClickContext context, CancellationToken ct = default)
    {
        var link = await links.FindByShortCodeAsync(shortCode, ct);

        if (link is null) return new RedirectNotFound();
        if (!IsActive(link)) return new RedirectGone();

        if (link.PasswordHash is null)
        {
            // Defensive: the link is not actually password-protected. Treat
            // this like a normal redirect rather than reject the request.
            await RecordClickAsync(link.Id, context, ct);
            return new RedirectFound(link.OriginalUrl);
        }

        if (!passwordHasher.Verify(password, link.PasswordHash))
        {
            return new RedirectPasswordRequired(LastAttemptFailed: true);
        }

        await RecordClickAsync(link.Id, context, ct);
        return new RedirectFound(link.OriginalUrl);
    }

    private static bool IsActive(ShortLink link) =>
        link.IsActive && (link.ExpiresAt is null || link.ExpiresAt > DateTime.UtcNow);

    private async Task RecordClickAsync(Guid shortLinkId, ClickContext context, CancellationToken ct)
    {
        var click = new Click
        {
            Id = Guid.NewGuid(),
            ShortLinkId = shortLinkId,
            ClickedAt = DateTime.UtcNow,
            IpAddress = context.IpAddress,
            UserAgent = context.UserAgent,
            Referrer = context.Referrer
            // Country, City, Browser, Os, Device, IsBot are filled by the
            // enrichment job in week 6.
        };

        await clicks.AddAsync(click, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
