using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using UrlShortener.Api.IntegrationTests.Fixtures;
using UrlShortener.Application.Auth.Dtos;

namespace UrlShortener.Api.IntegrationTests.RateLimiting;

public class RateLimitingTests(PostgresContainerFixture container) : IntegrationTestBase(container)
{
    [Fact]
    public async Task Auth_endpoint_returns_429_after_exceeding_per_ip_limit()
    {
        // Auth policy permits 10 requests per minute per IP. Eleven failed
        // logins from the same IP must be cut off on the eleventh.
        for (var i = 0; i < 10; i++)
        {
            var ok = await Client.PostAsJsonAsync("/api/auth/login",
                new LoginRequest($"none-{i}@example.com", "Wrong!"));
            ok.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var rejected = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("none-11@example.com", "Wrong!"));

        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Rate_limit_response_carries_problem_details_and_retry_after_header()
    {
        // Burn through the auth window first.
        for (var i = 0; i < 10; i++)
        {
            await Client.PostAsJsonAsync("/api/auth/login",
                new LoginRequest($"burn-{i}@example.com", "Wrong!"));
        }

        var rejected = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("burn-extra@example.com", "Wrong!"));

        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        rejected.Headers.RetryAfter.Should().NotBeNull();
        rejected.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var doc = await rejected.Content.ReadFromJsonAsync<JsonElement>();
        doc.GetProperty("status").GetInt32().Should().Be(429);
        doc.GetProperty("title").GetString().Should().Be("Too Many Requests");
    }

    [Fact]
    public async Task Different_ips_consume_separate_buckets()
    {
        // Exhaust this test's primary IP.
        for (var i = 0; i < 10; i++)
        {
            await Client.PostAsJsonAsync("/api/auth/login",
                new LoginRequest($"a-{i}@example.com", "Wrong!"));
        }

        // A second client posing as a different IP must still be allowed.
        var otherIpClient = Factory.CreateClient();
        otherIpClient.DefaultRequestHeaders.Add("X-Forwarded-For", "10.99.99.99");

        var firstFromOther = await otherIpClient.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("other@example.com", "Wrong!"));

        firstFromOther.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            because: "the other IP has its own bucket and the request only failed because credentials are invalid");
    }
}
