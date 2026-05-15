using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using UrlShortener.Application.Auth;
using UrlShortener.Application.Auth.Dtos;
using UrlShortener.Application.Auth.Interfaces;
using UrlShortener.Application.Auth.Services;
using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain.Exceptions;

namespace UrlShortener.Application.Tests.Auth;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _accessTokenGenerator = new();
    private readonly Mock<IRefreshTokenGenerator> _refreshTokenGenerator = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly JwtSettings _jwt = new()
    {
        Secret = "test-secret-please-ignore-not-used-by-mocks-xxxxxxxxxxxxx",
        Issuer = "test-issuer",
        Audience = "test-audience",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 30
    };

    private AuthService BuildSut() => new(
        _users.Object,
        _refreshTokens.Object,
        _passwordHasher.Object,
        _accessTokenGenerator.Object,
        _refreshTokenGenerator.Object,
        Options.Create(_jwt),
        _unitOfWork.Object);

    private void StubAccessToken(string token = "access.jwt", DateTime? expires = null) =>
        _accessTokenGenerator
            .Setup(g => g.GenerateAccessToken(It.IsAny<User>()))
            .Returns(new AccessTokenResult(token, expires ?? DateTime.UtcNow.AddMinutes(15)));

    private void StubRefreshGenerate(string raw = "raw-refresh", string hash = "hash-refresh") =>
        _refreshTokenGenerator
            .Setup(g => g.Generate())
            .Returns(new GeneratedRefreshToken(raw, hash));

    [Fact]
    public async Task RegisterAsync_persists_user_and_returns_token_pair_when_email_is_new()
    {
        _users.Setup(r => r.ExistsByEmailAsync("alice@example.com", It.IsAny<CancellationToken>()))
              .ReturnsAsync(false);
        _passwordHasher.Setup(h => h.Hash("P@ssw0rd!")).Returns("hashed-pw");
        StubAccessToken();
        StubRefreshGenerate();

        var sut = BuildSut();

        var response = await sut.RegisterAsync(new RegisterRequest("Alice@Example.com", "P@ssw0rd!"));

        response.AccessToken.Should().Be("access.jwt");
        response.RefreshToken.Should().Be("raw-refresh");
        response.User.Email.Should().Be("alice@example.com");

        _users.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.Email == "alice@example.com" && u.PasswordHash == "hashed-pw"), It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokens.Verify(r => r.AddAsync(It.Is<RefreshToken>(t =>
            t.TokenHash == "hash-refresh"), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_throws_EmailAlreadyExists_when_email_is_taken()
    {
        _users.Setup(r => r.ExistsByEmailAsync("taken@example.com", It.IsAny<CancellationToken>()))
              .ReturnsAsync(true);

        var sut = BuildSut();

        await sut.Invoking(s => s.RegisterAsync(new RegisterRequest("taken@example.com", "P@ssw0rd!")))
                 .Should().ThrowAsync<EmailAlreadyExistsException>();

        _users.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_returns_token_pair_when_credentials_are_valid()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "alice@example.com", PasswordHash = "stored-hash" };
        _users.Setup(r => r.FindByEmailAsync("alice@example.com", It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("P@ssw0rd!", "stored-hash")).Returns(true);
        StubAccessToken();
        StubRefreshGenerate();

        var sut = BuildSut();

        var response = await sut.LoginAsync(new LoginRequest("alice@example.com", "P@ssw0rd!"));

        response.AccessToken.Should().Be("access.jwt");
        response.RefreshToken.Should().Be("raw-refresh");
        response.User.Id.Should().Be(user.Id);
        _refreshTokens.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_throws_InvalidCredentials_when_email_is_unknown()
    {
        _users.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((User?)null);

        var sut = BuildSut();

        await sut.Invoking(s => s.LoginAsync(new LoginRequest("ghost@example.com", "P@ssw0rd!")))
                 .Should().ThrowAsync<InvalidCredentialsException>();

        _passwordHasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_throws_InvalidCredentials_when_password_is_wrong()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "alice@example.com", PasswordHash = "stored-hash" };
        _users.Setup(r => r.FindByEmailAsync("alice@example.com", It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("wrong", "stored-hash")).Returns(false);

        var sut = BuildSut();

        await sut.Invoking(s => s.LoginAsync(new LoginRequest("alice@example.com", "wrong")))
                 .Should().ThrowAsync<InvalidCredentialsException>();

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_rotates_token_and_returns_new_pair_when_token_is_active()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "alice@example.com", PasswordHash = "stored-hash" };
        var existing = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            TokenHash = "old-hash",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(29)
        };

        _refreshTokenGenerator.Setup(g => g.ComputeHash("old-raw")).Returns("old-hash");
        _refreshTokens.Setup(r => r.FindByTokenHashAsync("old-hash", It.IsAny<CancellationToken>()))
                      .ReturnsAsync(existing);
        StubAccessToken();
        StubRefreshGenerate("new-raw", "new-hash");

        var sut = BuildSut();

        var response = await sut.RefreshAsync("old-raw");

        response.AccessToken.Should().Be("access.jwt");
        response.RefreshToken.Should().Be("new-raw");
        existing.RevokedAt.Should().NotBeNull();
        existing.ReplacedByTokenHash.Should().Be("new-hash");
        _refreshTokens.Verify(r => r.AddAsync(It.Is<RefreshToken>(t => t.TokenHash == "new-hash"),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_throws_InvalidRefreshToken_when_token_is_unknown()
    {
        _refreshTokenGenerator.Setup(g => g.ComputeHash(It.IsAny<string>())).Returns("any-hash");
        _refreshTokens.Setup(r => r.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((RefreshToken?)null);

        var sut = BuildSut();

        await sut.Invoking(s => s.RefreshAsync("anything"))
                 .Should().ThrowAsync<InvalidRefreshTokenException>();

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_throws_InvalidRefreshToken_when_token_is_revoked()
    {
        var revoked = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            User = new User { Id = Guid.NewGuid(), Email = "x@example.com", PasswordHash = "h" },
            TokenHash = "h",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            ExpiresAt = DateTime.UtcNow.AddDays(28),
            RevokedAt = DateTime.UtcNow.AddDays(-1)
        };
        _refreshTokenGenerator.Setup(g => g.ComputeHash("raw")).Returns("h");
        _refreshTokens.Setup(r => r.FindByTokenHashAsync("h", It.IsAny<CancellationToken>()))
                      .ReturnsAsync(revoked);

        var sut = BuildSut();

        await sut.Invoking(s => s.RefreshAsync("raw"))
                 .Should().ThrowAsync<InvalidRefreshTokenException>();

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_revokes_token_when_active()
    {
        var active = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            User = new User { Id = Guid.NewGuid(), Email = "x@example.com", PasswordHash = "h" },
            TokenHash = "h",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(29)
        };
        _refreshTokenGenerator.Setup(g => g.ComputeHash("raw")).Returns("h");
        _refreshTokens.Setup(r => r.FindByTokenHashAsync("h", It.IsAny<CancellationToken>()))
                      .ReturnsAsync(active);

        var sut = BuildSut();

        await sut.LogoutAsync("raw");

        active.RevokedAt.Should().NotBeNull();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_is_a_noop_when_token_is_missing_or_revoked()
    {
        _refreshTokenGenerator.Setup(g => g.ComputeHash(It.IsAny<string>())).Returns("h");
        _refreshTokens.Setup(r => r.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((RefreshToken?)null);

        var sut = BuildSut();

        await sut.LogoutAsync("anything");

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
