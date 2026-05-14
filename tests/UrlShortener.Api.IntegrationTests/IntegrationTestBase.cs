using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Api.IntegrationTests.Fixtures;
using UrlShortener.Application.Auth.Dtos;
using UrlShortener.Infrastructure.Persistence;

namespace UrlShortener.Api.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgresContainerFixture _container;
    protected ApiFactory Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;

    protected IntegrationTestBase(PostgresContainerFixture container)
    {
        _container = container;
    }

    public async Task InitializeAsync()
    {
        Factory = new ApiFactory(_container.ConnectionString);
        await Factory.EnsureMigratedAsync();
        Client = Factory.CreateClient();
        await ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Factory.DisposeAsync().AsTask();
    }

    // Truncates every domain table between tests so each one starts from a
    // clean slate. CASCADE handles the FKs from RefreshTokens and Clicks.
    private async Task ResetDatabaseAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            """TRUNCATE TABLE "RefreshTokens", "Clicks", "ShortLinks", "Users" RESTART IDENTITY CASCADE;""");
    }

    // Registers a fresh user and returns both the auth response and an
    // HttpClient with the bearer token preset. Lets each test get an
    // independent authenticated context in one line.
    protected async Task<(AuthResponse Auth, HttpClient AuthedClient)> RegisterAndAuthenticateAsync(
        string email,
        string password = "P@ssw0rd!")
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, password));
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (auth, client);
    }
}
