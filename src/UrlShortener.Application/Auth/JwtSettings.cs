namespace UrlShortener.Application.Auth;

// Bound from configuration section "Jwt". Values are validated at startup
// in Program.cs before the host is built.
public class JwtSettings
{
    public string Secret { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
}
