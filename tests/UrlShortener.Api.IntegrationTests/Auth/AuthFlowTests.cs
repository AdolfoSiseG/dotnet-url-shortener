using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using UrlShortener.Api.IntegrationTests.Fixtures;
using UrlShortener.Application.Auth.Dtos;

namespace UrlShortener.Api.IntegrationTests.Auth;

public class AuthFlowTests(PostgresContainerFixture container) : IntegrationTestBase(container)
{
    [Fact]
    public async Task Register_returns_409_when_email_is_already_taken()
    {
        await Client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("dup@example.com", "P@ssw0rd!"));

        var second = await Client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("dup@example.com", "AnotherP@ss1"));

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_returns_token_pair_for_valid_credentials()
    {
        await Client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("login@example.com", "P@ssw0rd!"));

        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("login@example.com", "P@ssw0rd!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth!.AccessToken.Should().NotBeNullOrWhiteSpace();
        auth.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_returns_401_for_wrong_password()
    {
        await Client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("badpw@example.com", "P@ssw0rd!"));

        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("badpw@example.com", "Wrong123!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_rotates_to_a_new_token_pair()
    {
        var (auth, _) = await RegisterAndAuthenticateAsync("rot@example.com");

        var response = await Client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshRequest(auth.RefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotated = await response.Content.ReadFromJsonAsync<AuthResponse>();
        rotated!.RefreshToken.Should().NotBe(auth.RefreshToken);
        rotated.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Refresh_with_an_already_rotated_token_returns_401()
    {
        var (auth, _) = await RegisterAndAuthenticateAsync("reuse@example.com");

        await Client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(auth.RefreshToken));

        var second = await Client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshRequest(auth.RefreshToken));

        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_invalidates_the_refresh_token()
    {
        var (auth, _) = await RegisterAndAuthenticateAsync("out@example.com");

        var logout = await Client.PostAsJsonAsync("/api/auth/logout",
            new LogoutRequest(auth.RefreshToken));
        logout.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        var refresh = await Client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshRequest(auth.RefreshToken));
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
