using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Auth.Interfaces;

public interface IJwtTokenGenerator
{
    AccessTokenResult GenerateAccessToken(User user);
}

public record AccessTokenResult(string Token, DateTime ExpiresAt);
