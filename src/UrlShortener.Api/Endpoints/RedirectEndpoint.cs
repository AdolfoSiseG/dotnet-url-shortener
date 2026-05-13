using System.Diagnostics;
using UrlShortener.Api.Templates;
using UrlShortener.Application.Redirects.Interfaces;
using UrlShortener.Application.Redirects.Models;

namespace UrlShortener.Api.Endpoints;

// Hot path of the URL shortener. Defined as minimal APIs (not controllers)
// to keep request handling as light as possible: every redirect goes through
// this code.
public static class RedirectEndpoint
{
    public static IEndpointRouteBuilder MapRedirect(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/{shortCode}", HandleGet)
            .WithName("Redirect")
            .ExcludeFromDescription();

        endpoints.MapPost("/{shortCode}", HandlePost)
            .WithName("RedirectUnlock")
            .ExcludeFromDescription()
            .DisableAntiforgery();

        return endpoints;
    }

    private static async Task<IResult> HandleGet(
        string shortCode,
        HttpContext context,
        IRedirectService redirect,
        CancellationToken ct)
    {
        var clickContext = BuildClickContext(context);
        var result = await redirect.ResolveAsync(shortCode, clickContext, ct);
        return ToHttpResult(result, shortCode);
    }

    private static async Task<IResult> HandlePost(
        string shortCode,
        HttpContext context,
        IRedirectService redirect,
        CancellationToken ct)
    {
        var form = await context.Request.ReadFormAsync(ct);
        var password = form["password"].ToString();

        var clickContext = BuildClickContext(context);
        var result = await redirect.UnlockAsync(shortCode, password, clickContext, ct);
        return ToHttpResult(result, shortCode);
    }

    private static IResult ToHttpResult(RedirectResult result, string shortCode) =>
        result switch
        {
            RedirectFound found => Results.Redirect(found.TargetUrl, permanent: true),
            RedirectGone => Results.StatusCode(StatusCodes.Status410Gone),
            RedirectNotFound => Results.NotFound(),
            RedirectPasswordRequired prompt => Results.Content(
                PasswordUnlockPage.Render(shortCode, prompt.LastAttemptFailed),
                "text/html; charset=utf-8"),
            _ => throw new UnreachableException($"Unhandled redirect result: {result.GetType().Name}")
        };

    private static ClickContext BuildClickContext(HttpContext context)
    {
        var userAgent = context.Request.Headers.UserAgent.ToString();
        if (userAgent.Length > 512) userAgent = userAgent[..512];
        if (string.IsNullOrEmpty(userAgent)) userAgent = "unknown";

        var referer = context.Request.Headers.Referer.ToString();
        if (referer.Length > 2048) referer = referer[..2048];

        return new ClickContext(
            IpAddress: GetClientIp(context),
            UserAgent: userAgent,
            Referrer: string.IsNullOrEmpty(referer) ? null : referer);
    }

    private static string GetClientIp(HttpContext context)
    {
        // Prefer X-Forwarded-For (set by reverse proxies in production) over
        // the socket-level RemoteIpAddress. The header is a comma-separated
        // list with the original client first. Trust hardening (KnownProxies)
        // is added in the deploy week.
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
