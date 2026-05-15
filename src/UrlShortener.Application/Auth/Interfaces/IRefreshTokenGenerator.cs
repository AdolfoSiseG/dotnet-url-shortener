namespace UrlShortener.Application.Auth.Interfaces;

public interface IRefreshTokenGenerator
{
    GeneratedRefreshToken Generate();

    // Used to look up a token submitted by a client: hash the incoming raw
    // token, then query the repository by hash.
    string ComputeHash(string rawToken);
}

public record GeneratedRefreshToken(string Token, string Hash);
