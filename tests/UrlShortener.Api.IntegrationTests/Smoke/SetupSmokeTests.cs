using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using UrlShortener.Api.IntegrationTests.Fixtures;
using UrlShortener.Application.Auth.Dtos;

namespace UrlShortener.Api.IntegrationTests.Smoke;

// Smoke tests for the integration test harness itself: container starts,
// migrations apply, factory wires the testing config, and auth pipeline is
// active. If these break, the rest of the suite is meaningless.
public class SetupSmokeTests(PostgresContainerFixture container) : IntegrationTestBase(container)
{
    [Fact]
    public async Task Register_returns_token_pair_and_persists_user()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("smoke@example.com", "P@ssw0rd!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.User.Email.Should().Be("smoke@example.com");
    }

    [Fact]
    public async Task Get_links_without_token_returns_401()
    {
        var response = await Client.GetAsync("/api/links");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
