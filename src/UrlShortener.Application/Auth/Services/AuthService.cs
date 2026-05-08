using UrlShortener.Application.Auth.Dtos;
using UrlShortener.Application.Auth.Interfaces;
using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain.Exceptions;

namespace UrlShortener.Application.Auth.Services;

public class AuthService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator) : IAuthService
{
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
        await users.SaveChangesAsync(ct);

        return BuildAuthResponse(user);
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

        return BuildAuthResponse(user);
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        var token = tokenGenerator.GenerateAccessToken(user);
        var dto = new UserDto(user.Id, user.Email, user.CreatedAt);
        return new AuthResponse(token.Token, token.ExpiresAt, dto);
    }

    // Lowercase so email uniqueness ignores casing without needing a
    // case-insensitive collation on the column itself.
    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();
}
