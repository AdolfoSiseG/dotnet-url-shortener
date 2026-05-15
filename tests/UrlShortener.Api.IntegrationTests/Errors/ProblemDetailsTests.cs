using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using UrlShortener.Api.IntegrationTests.Fixtures;
using UrlShortener.Application.Auth.Dtos;

namespace UrlShortener.Api.IntegrationTests.Errors;

public class ProblemDetailsTests(PostgresContainerFixture container) : IntegrationTestBase(container)
{
    [Fact]
    public async Task Login_with_wrong_password_returns_problem_details_body()
    {
        await Client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("pd-login@example.com", "P@ssw0rd!"));

        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("pd-login@example.com", "Wrong!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var doc = await response.Content.ReadFromJsonAsync<JsonElement>();
        doc.GetProperty("status").GetInt32().Should().Be(401);
        doc.GetProperty("title").GetString().Should().Be("Unauthorized");
        doc.GetProperty("detail").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_with_duplicate_email_returns_problem_details_body()
    {
        await Client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("pd-dup@example.com", "P@ssw0rd!"));

        var response = await Client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("pd-dup@example.com", "P@ssw0rd!"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var doc = await response.Content.ReadFromJsonAsync<JsonElement>();
        doc.GetProperty("status").GetInt32().Should().Be(409);
        doc.GetProperty("title").GetString().Should().Be("Conflict");
    }
}
