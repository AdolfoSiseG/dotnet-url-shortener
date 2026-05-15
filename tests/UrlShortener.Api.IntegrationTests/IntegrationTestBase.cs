using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
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

    // Each test gets a synthetic IP routed via X-Forwarded-For. The auth
    // and redirect rate limiters partition by IP, so a unique value here
    // guarantees no leak of rate-limit state from one test to the next.
    protected string TestIp { get; private set; } = string.Empty;

    protected IntegrationTestBase(PostgresContainerFixture container)
    {
        _container = container;
    }

    public async Task InitializeAsync()
    {
        Factory = new ApiFactory(_container.ConnectionString);
        await Factory.EnsureMigratedAsync();
        TestIp = NewSyntheticIp();
        Client = Factory.CreateClient();
        Client.DefaultRequestHeaders.Add("X-Forwarded-For", TestIp);
        await ResetDatabaseAsync();
    }

    private static string NewSyntheticIp() =>
        $"10.{Random.Shared.Next(1, 255)}.{Random.Shared.Next(0, 255)}.{Random.Shared.Next(1, 255)}";

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
        client.DefaultRequestHeaders.Add("X-Forwarded-For", TestIp);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (auth, client);
    }

    // The default WebApplicationFactory client follows redirects, which
    // hides the 301 status from the redirect endpoint. Use this client
    // when the test needs to assert on the redirect response itself.
    protected HttpClient CreateClientNoRedirect()
    {
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", TestIp);
        return client;
    }

    // Opens a fresh DbContext scope so a test can mutate state that the API
    // does not allow through its public surface (e.g. backdating ExpiresAt).
    protected async Task WithDbAsync(Func<AppDbContext, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(db);
    }
}
