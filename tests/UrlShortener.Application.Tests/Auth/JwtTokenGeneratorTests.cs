using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Options;
using UrlShortener.Application.Auth;
using UrlShortener.Domain.Entities;
using UrlShortener.Infrastructure.Auth;

namespace UrlShortener.Application.Tests.Auth;

public class JwtTokenGeneratorTests
{
    private static JwtTokenGenerator BuildGenerator()
    {
        var settings = new JwtSettings
        {
            Secret = "test-only-secret-must-be-at-least-32-bytes-long-aaaa",
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenMinutes = 15
        };

        return new JwtTokenGenerator(Options.Create(settings));
    }

    private static User BuildUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "user@example.com",
        PasswordHash = "irrelevant",
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public void GenerateAccessToken_returns_a_well_formed_jwt()
    {
        var token = BuildGenerator().GenerateAccessToken(BuildUser());

        token.Token.Should().NotBeNullOrEmpty();
        token.Token.Split('.').Should().HaveCount(3);  // header.payload.signature
    }

    [Fact]
    public void GenerateAccessToken_includes_user_id_as_sub_claim()
    {
        var user = BuildUser();
        var token = BuildGenerator().GenerateAccessToken(user);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token.Token);

        parsed.Subject.Should().Be(user.Id.ToString());
    }

    [Fact]
    public void GenerateAccessToken_includes_email_claim()
    {
        var user = BuildUser();
        var token = BuildGenerator().GenerateAccessToken(user);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token.Token);

        parsed.Claims.Should()
            .Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
    }

    [Fact]
    public void GenerateAccessToken_sets_expiry_in_the_future()
    {
        var token = BuildGenerator().GenerateAccessToken(BuildUser());

        token.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        token.ExpiresAt.Should().BeBefore(DateTime.UtcNow.AddMinutes(20));
    }

    [Fact]
    public void GenerateAccessToken_uses_configured_issuer_and_audience()
    {
        var token = BuildGenerator().GenerateAccessToken(BuildUser());

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token.Token);

        parsed.Issuer.Should().Be("test-issuer");
        parsed.Audiences.Should().Contain("test-audience");
    }
}
