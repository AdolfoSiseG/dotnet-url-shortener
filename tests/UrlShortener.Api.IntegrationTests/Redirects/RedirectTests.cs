using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.IntegrationTests.Fixtures;
using UrlShortener.Application.Links.Dtos;

namespace UrlShortener.Api.IntegrationTests.Redirects;

public class RedirectTests(PostgresContainerFixture container) : IntegrationTestBase(container)
{
    [Fact]
    public async Task Get_returns_404_for_unknown_short_code()
    {
        var client = CreateClientNoRedirect();

        var response = await client.GetAsync("/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_returns_301_with_target_url_for_active_link()
    {
        var (_, authed) = await RegisterAndAuthenticateAsync("redir@example.com");
        var link = await CreateAsync(authed, "https://example.com/landing");

        var client = CreateClientNoRedirect();
        var response = await client.GetAsync($"/{link.ShortCode}");

        response.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        response.Headers.Location!.ToString().Should().Be("https://example.com/landing");
    }

    [Fact]
    public async Task Get_returns_410_for_inactive_link()
    {
        var (_, authed) = await RegisterAndAuthenticateAsync("inactive@example.com");
        var link = await CreateAsync(authed, "https://example.com/dead");

        await authed.PatchAsJsonAsync($"/api/links/{link.Id}",
            new UpdateShortLinkRequest(IsActive: false));

        var client = CreateClientNoRedirect();
        var response = await client.GetAsync($"/{link.ShortCode}");

        response.StatusCode.Should().Be(HttpStatusCode.Gone);
    }

    [Fact]
    public async Task Get_returns_410_for_expired_link()
    {
        var (_, authed) = await RegisterAndAuthenticateAsync("expired@example.com");
        var link = await CreateAsync(authed, "https://example.com/old");

        // The validator forbids past ExpiresAt on create/update, so backdate
        // straight in the database to simulate a link whose deadline passed.
        await WithDbAsync(async db =>
        {
            var entity = await db.ShortLinks.SingleAsync(l => l.Id == link.Id);
            entity.ExpiresAt = DateTime.UtcNow.AddMinutes(-5);
            await db.SaveChangesAsync();
        });

        var client = CreateClientNoRedirect();
        var response = await client.GetAsync($"/{link.ShortCode}");

        response.StatusCode.Should().Be(HttpStatusCode.Gone);
    }

    [Fact]
    public async Task Get_returns_html_unlock_form_for_password_protected_link()
    {
        var (_, authed) = await RegisterAndAuthenticateAsync("protected@example.com");
        var link = await CreateAsync(authed, "https://example.com/secret", password: "letmein");

        var client = CreateClientNoRedirect();
        var response = await client.GetAsync($"/{link.ShortCode}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("password", because: "the unlock form must include a password input");
    }

    [Fact]
    public async Task Post_with_correct_password_returns_301()
    {
        var (_, authed) = await RegisterAndAuthenticateAsync("unlock@example.com");
        var link = await CreateAsync(authed, "https://example.com/treasure", password: "letmein");

        var client = CreateClientNoRedirect();
        var form = new FormUrlEncodedContent([new KeyValuePair<string, string>("password", "letmein")]);
        var response = await client.PostAsync($"/{link.ShortCode}", form);

        response.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        response.Headers.Location!.ToString().Should().Be("https://example.com/treasure");
    }

    [Fact]
    public async Task Post_with_wrong_password_returns_html_form_again()
    {
        var (_, authed) = await RegisterAndAuthenticateAsync("badpw-redir@example.com");
        var link = await CreateAsync(authed, "https://example.com/treasure2", password: "letmein");

        var client = CreateClientNoRedirect();
        var form = new FormUrlEncodedContent([new KeyValuePair<string, string>("password", "wrong")]);
        var response = await client.PostAsync($"/{link.ShortCode}", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
    }

    private static async Task<ShortLinkDto> CreateAsync(HttpClient client, string url, string? password = null)
    {
        var response = await client.PostAsJsonAsync("/api/links",
            new CreateShortLinkRequest(url, Password: password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ShortLinkDto>())!;
    }
}
