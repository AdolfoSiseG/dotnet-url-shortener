namespace UrlShortener.Api.RateLimiting;

// Resolves the client IP for partition keys and click capture. Behind a
// reverse proxy, X-Forwarded-For carries the original client (the leftmost
// entry); the socket-level RemoteIpAddress would only see the proxy.
// Trust hardening (KnownProxies / KnownNetworks) is added in the deploy week.
public static class ClientIpResolver
{
    public const string UnknownIp = "unknown";

    public static string Resolve(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }
        return context.Connection.RemoteIpAddress?.ToString() ?? UnknownIp;
    }
}
