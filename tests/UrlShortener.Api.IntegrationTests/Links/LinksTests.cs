using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using UrlShortener.Api.IntegrationTests.Fixtures;
using UrlShortener.Application.Common.Models;
using UrlShortener.Application.Links.Dtos;

namespace UrlShortener.Api.IntegrationTests.Links;

public class LinksTests(PostgresContainerFixture container) : IntegrationTestBase(container)
{
    [Fact]
    public async Task Create_returns_201_with_dto_for_a_valid_request()
    {
        var (_, client) = await RegisterAndAuthenticateAsync("creator@example.com");

        var response = await client.PostAsJsonAsync("/api/links",
            new CreateShortLinkRequest("https://example.com/landing", Title: "Landing"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<ShortLinkDto>();
        dto!.OriginalUrl.Should().Be("https://example.com/landing");
        dto.Title.Should().Be("Landing");
        dto.ShortCode.Should().NotBeNullOrWhiteSpace();
        dto.IsActive.Should().BeTrue();
        dto.HasPassword.Should().BeFalse();
    }

    [Fact]
    public async Task Create_returns_400_when_url_is_not_absolute_http()
    {
        var (_, client) = await RegisterAndAuthenticateAsync("badurl@example.com");

        var response = await client.PostAsJsonAsync("/api/links",
            new CreateShortLinkRequest("not-a-url"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_paginates_owners_links()
    {
        var (_, client) = await RegisterAndAuthenticateAsync("lister@example.com");

        for (var i = 0; i < 3; i++)
        {
            await client.PostAsJsonAsync("/api/links",
                new CreateShortLinkRequest($"https://example.com/{i}"));
        }

        var response = await client.GetAsync("/api/links?page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PaginatedResult<ShortLinkDto>>();
        page!.Items.Should().HaveCount(2);
        page.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Update_changes_title_for_owners_link()
    {
        var (_, client) = await RegisterAndAuthenticateAsync("editor@example.com");
        var created = await CreateAsync(client, "https://example.com/x");

        var response = await client.PatchAsJsonAsync($"/api/links/{created.Id}",
            new UpdateShortLinkRequest(Title: "Renamed"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ShortLinkDto>();
        updated!.Title.Should().Be("Renamed");
    }

    [Fact]
    public async Task Delete_soft_deletes_and_subsequent_get_returns_404()
    {
        var (_, client) = await RegisterAndAuthenticateAsync("deleter@example.com");
        var created = await CreateAsync(client, "https://example.com/y");

        var delete = await client.DeleteAsync($"/api/links/{created.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var followUp = await client.GetAsync($"/api/links/{created.Id}");
        followUp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_returns_404_when_link_belongs_to_a_different_user()
    {
        var (_, alice) = await RegisterAndAuthenticateAsync("alice@example.com");
        var (_, bob) = await RegisterAndAuthenticateAsync("bob@example.com");
        var aliceLink = await CreateAsync(alice, "https://alice.example.com/secret");

        var response = await bob.GetAsync($"/api/links/{aliceLink.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_returns_404_when_link_belongs_to_a_different_user()
    {
        var (_, alice) = await RegisterAndAuthenticateAsync("a2@example.com");
        var (_, bob) = await RegisterAndAuthenticateAsync("b2@example.com");
        var aliceLink = await CreateAsync(alice, "https://alice.example.com/secret2");

        var response = await bob.DeleteAsync($"/api/links/{aliceLink.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Confirm Alice's link survived Bob's attempt.
        var aliceFollowUp = await alice.GetAsync($"/api/links/{aliceLink.Id}");
        aliceFollowUp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<ShortLinkDto> CreateAsync(HttpClient client, string url)
    {
        var response = await client.PostAsJsonAsync("/api/links", new CreateShortLinkRequest(url));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ShortLinkDto>())!;
    }
}
