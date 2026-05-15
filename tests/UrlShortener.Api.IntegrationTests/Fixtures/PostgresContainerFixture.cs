using Testcontainers.PostgreSql;

namespace UrlShortener.Api.IntegrationTests.Fixtures;

// One Postgres container shared by every test in the assembly via the
// xUnit collection in IntegrationTestCollection.cs. Spinning up Postgres
// costs ~5s on a warm Docker daemon, so amortizing it across all tests
// keeps the suite fast.
public class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .WithDatabase("urlshortener_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
