using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using UrlShortener.Api.IntegrationTests.Fixtures;

namespace UrlShortener.Api.IntegrationTests.OpenApi;

public class OpenApiDocumentTests(PostgresContainerFixture container) : IntegrationTestBase(container)
{
    [Fact]
    public async Task OpenApi_document_is_served_and_includes_jwt_bearer_security_scheme()
    {
        var response = await Client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = await response.Content.ReadFromJsonAsync<JsonElement>();
        var bearer = doc
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer");

        bearer.GetProperty("type").GetString().Should().Be("http");
        bearer.GetProperty("scheme").GetString().Should().Be("bearer");
        bearer.GetProperty("bearerFormat").GetString().Should().Be("JWT");
    }
}
