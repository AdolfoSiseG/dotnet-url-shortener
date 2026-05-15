using System.Security.Cryptography;
using System.Text;
using UrlShortener.Application.Auth.Interfaces;

namespace UrlShortener.Infrastructure.Auth;

public class RefreshTokenGenerator : IRefreshTokenGenerator
{
    // 64 random bytes = 512 bits of entropy. Far beyond brute-force range
    // for any practical attacker; keeps the same token format if we move to
    // a longer hash later.
    private const int TokenByteLength = 64;

    public GeneratedRefreshToken Generate()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenByteLength));
        return new GeneratedRefreshToken(raw, ComputeHash(raw));
    }

    public string ComputeHash(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        // Hex over base64 for readability when inspecting the database.
        return Convert.ToHexString(hash);
    }
}
