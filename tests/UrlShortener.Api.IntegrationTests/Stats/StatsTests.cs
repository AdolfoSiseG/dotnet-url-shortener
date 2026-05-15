using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using UrlShortener.Api.IntegrationTests.Fixtures;
using UrlShortener.Application.Analytics.Dtos;
using UrlShortener.Application.Links.Dtos;

namespace UrlShortener.Api.IntegrationTests.Stats;

public class StatsTests(PostgresContainerFixture container) : IntegrationTestBase(container)
{
    [Fact]
    public async Task Overview_requires_authentication()
    {
        var response = await Client.GetAsync("/api/stats/overview");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Overview_returns_users_link_count_and_active_count()
    {
        var (_, authed) = await RegisterAndAuthenticateAsync("over@example.com");
        await CreateAsync(authed, "https://example.com/a");
        var second = await CreateAsync(authed, "https://example.com/b");

        await authed.PatchAsJsonAsync($"/api/links/{second.Id}",
            new UpdateShortLinkRequest(IsActive: false));

        var response = await authed.GetAsync("/api/stats/overview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<OverviewStatsDto>();
        stats!.TotalLinks.Should().Be(2);
        stats.ActiveLinks.Should().Be(1);
        stats.TotalClicks.Should().Be(0);
        stats.TopLinks.Should().HaveCount(2);
    }

    [Fact]
    public async Task Overview_does_not_count_links_owned_by_other_users()
    {
        var (_, alice) = await RegisterAndAuthenticateAsync("a-stats@example.com");
        var (_, bob) = await RegisterAndAuthenticateAsync("b-stats@example.com");

        await CreateAsync(alice, "https://alice.example.com/1");
        await CreateAsync(alice, "https://alice.example.com/2");
        await CreateAsync(bob, "https://bob.example.com/1");

        var response = await bob.GetAsync("/api/stats/overview");
        var stats = await response.Content.ReadFromJsonAsync<OverviewStatsDto>();

        stats!.TotalLinks.Should().Be(1);
    }

    [Fact]
    public async Task By_country_returns_empty_for_user_with_no_clicks()
    {
        var (_, authed) = await RegisterAndAuthenticateAsync("nocountry@example.com");
        await CreateAsync(authed, "https://example.com/empty");

        var response = await authed.GetAsync("/api/stats/by-country");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rows = await response.Content.ReadFromJsonAsync<IReadOnlyList<CountryStatDto>>();
        rows.Should().BeEmpty();
    }

    [Fact]
    public async Task By_time_returns_400_when_granularity_is_invalid()
    {
        var (_, authed) = await RegisterAndAuthenticateAsync("badgran@example.com");
        var from = DateTime.UtcNow.AddDays(-7).ToString("o");
        var to = DateTime.UtcNow.ToString("o");

        var response = await authed.GetAsync($"/api/stats/by-time?from={from}&to={to}&granularity=hour");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<ShortLinkDto> CreateAsync(HttpClient client, string url)
    {
        var response = await client.PostAsJsonAsync("/api/links", new CreateShortLinkRequest(url));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ShortLinkDto>())!;
    }
}
