using Microsoft.Extensions.Options;
using UrlShortener.Application.Auth.Dtos;
using UrlShortener.Application.Auth.Interfaces;
using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain.Exceptions;

namespace UrlShortener.Application.Auth.Services;

public class AuthService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator accessTokenGenerator,
    IRefreshTokenGenerator refreshTokenGenerator,
    IOptions<JwtSettings> jwtOptions,
    IUnitOfWork unitOfWork) : IAuthService
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = NormalizeEmail(request.Email);

        if (await users.ExistsByEmailAsync(email, ct))
        {
            throw new EmailAlreadyExistsException(email);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        await users.AddAsync(user, ct);
        var refresh = await IssueRefreshTokenAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return BuildAuthResponse(user, refresh);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = NormalizeEmail(request.Email);
        var user = await users.FindByEmailAsync(email, ct);

        // Single rejection path for both unknown email and wrong password so
        // an attacker cannot use timing or response shape to enumerate users.
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        var refresh = await IssueRefreshTokenAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return BuildAuthResponse(user, refresh);
    }

    public async Task<AuthResponse> RefreshAsync(string rawRefreshToken, CancellationToken ct = default)
    {
        var hash = refreshTokenGenerator.ComputeHash(rawRefreshToken);
        var existing = await refreshTokens.FindByTokenHashAsync(hash, ct);

        if (existing is null || !existing.IsActive)
        {
            // In a hardened build we'd revoke the entire token chain on reuse
            // of an already-revoked token (stolen-token detection). v1.0 just
            // rejects.
            throw new InvalidRefreshTokenException();
        }

        // Rotate: revoke the old token and issue a fresh pair in one transaction.
        var newRefresh = await IssueRefreshTokenAsync(existing.User, ct);
        existing.RevokedAt = DateTime.UtcNow;
        existing.ReplacedByTokenHash = newRefresh.Hash;

        await unitOfWork.SaveChangesAsync(ct);

        return BuildAuthResponse(existing.User, newRefresh);
    }

    public async Task LogoutAsync(string rawRefreshToken, CancellationToken ct = default)
    {
        var hash = refreshTokenGenerator.ComputeHash(rawRefreshToken);
        var token = await refreshTokens.FindByTokenHashAsync(hash, ct);

        // Idempotent: revoking a missing or already-revoked token is a no-op
        // so logout is safe to retry.
        if (token is null || token.IsRevoked)
        {
            return;
        }

        token.RevokedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<GeneratedRefreshToken> IssueRefreshTokenAsync(User user, CancellationToken ct)
    {
        var generated = refreshTokenGenerator.Generate();
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = generated.Hash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays),
            CreatedAt = DateTime.UtcNow
        };

        await refreshTokens.AddAsync(token, ct);
        return generated;
    }

    private AuthResponse BuildAuthResponse(User user, GeneratedRefreshToken refresh)
    {
        var access = accessTokenGenerator.GenerateAccessToken(user);
        var dto = new UserDto(user.Id, user.Email, user.CreatedAt);
        return new AuthResponse(access.Token, access.ExpiresAt, refresh.Token, dto);
    }

    // Lowercase so email uniqueness ignores casing without needing a
    // case-insensitive collation on the column itself.
    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
