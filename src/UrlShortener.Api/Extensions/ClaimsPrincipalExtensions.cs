using System.Security.Claims;

namespace UrlShortener.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    // The JWT bearer middleware maps the 'sub' claim to ClaimTypes.NameIdentifier
    // by default. This helper centralizes the parse so controllers don't repeat
    // the same five lines on every action.
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User identity has no NameIdentifier claim.");
        return Guid.Parse(raw);
    }
}
