using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Infrastructure.Persistence;

namespace UrlShortener.Api.IntegrationTests.Fixtures;

// WebApplicationFactory specialization that points the API at the
// Testcontainers Postgres, disables Hangfire, and replaces outbound
// integrations (geo lookup, enrichment scheduler) with deterministic fakes.
//
// We use UseSetting (not ConfigureAppConfiguration) for the config overrides
// because WebApplicationBuilder freezes its Configuration before the
// ConfigureAppConfiguration hook runs — UseSetting injects into the
// IWebHostBuilder which Program.cs reads at construction.
public class ApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:Default", connectionString);
        builder.UseSetting("Hangfire:Enabled", "false");
        builder.UseSetting("Jwt:Secret", "integration-tests-secret-not-used-in-production-xxxxxxxxxxx");
        builder.UseSetting("Jwt:Issuer", "url-shortener-tests");
        builder.UseSetting("Jwt:Audience", "url-shortener-tests");
        builder.UseSetting("Jwt:AccessTokenMinutes", "15");
        builder.UseSetting("Jwt:RefreshTokenDays", "30");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IIpGeolocationService>();
            services.AddSingleton<IIpGeolocationService, FakeIpGeolocationService>();

            services.RemoveAll<IClickEnrichmentScheduler>();
            services.AddScoped<IClickEnrichmentScheduler, NoOpClickEnrichmentScheduler>();
        });
    }

    public async Task EnsureMigratedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}
