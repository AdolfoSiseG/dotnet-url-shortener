using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Api.IntegrationTests.Fixtures;
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
}
