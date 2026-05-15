using UrlShortener.Application.Auth.Interfaces;

namespace UrlShortener.Infrastructure.Auth;

public class BcryptPasswordHasher : IPasswordHasher
{
    // Each +1 to the work factor doubles hash cost. 12 is the OWASP-recommended
    // minimum and keeps a single hash under ~250ms on modern hardware.
    private const int WorkFactor = 12;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
