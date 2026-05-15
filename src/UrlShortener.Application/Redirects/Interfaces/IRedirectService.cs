using UrlShortener.Application.Redirects.Models;

namespace UrlShortener.Application.Redirects.Interfaces;

public interface IRedirectService
{
    // Handles a public GET /{shortCode}. Captures a click row on every
    // successful redirect; does not capture for gone/not-found/password-prompt.
    Task<RedirectResult> ResolveAsync(string shortCode, ClickContext context, CancellationToken ct = default);

    // Handles the POST that submits a password for a protected link.
    Task<RedirectResult> UnlockAsync(string shortCode, string password, ClickContext context, CancellationToken ct = default);
}
