using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using UrlShortener.Application.Analytics.Interfaces;
using UrlShortener.Application.Auth;
using UrlShortener.Application.Auth.Interfaces;
using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Application.Links.Interfaces;
using UrlShortener.Infrastructure.Analytics;
using UrlShortener.Infrastructure.Auth;
using UrlShortener.Infrastructure.Geolocation;
using UrlShortener.Infrastructure.Jobs;
using UrlShortener.Infrastructure.Persistence;
using UrlShortener.Infrastructure.Persistence.Repositories;
using UrlShortener.Infrastructure.QrCodes;
using UrlShortener.Infrastructure.ShortCodes;
using UrlShortener.Infrastructure.UserAgents;

namespace UrlShortener.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var rawConnectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
        var connectionString = NpgsqlConnectionString.FromRaw(rawConnectionString);

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IShortLinkRepository, ShortLinkRepository>();
        services.AddScoped<IClickRepository, ClickRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddMemoryCache();
        services.AddSingleton<IQrCodeGenerator, QrCodeGenerator>();

        // Analytics is registered as the concrete type and wrapped by the
        // cached decorator. Anyone resolving IAnalyticsService gets the
        // decorator; only the decorator resolves the concrete service.
        services.AddScoped<AnalyticsService>();
        services.AddScoped<IAnalyticsService>(sp => new CachedAnalyticsService(
            sp.GetRequiredService<AnalyticsService>(),
            sp.GetRequiredService<IMemoryCache>()));
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddSingleton<IShortCodeGenerator, ShortCodeGenerator>();
        services.AddSingleton<IUserAgentParser, UAParserAdapter>();

        // Click enrichment: the scheduler is the seam that keeps the
        // Application layer free of Hangfire types. The job itself is a
        // scoped service so its DbContext lifetime aligns with the request
        // (or with the Hangfire worker's per-invocation scope).
        services.AddScoped<IClickEnrichmentJob, ClickEnrichmentJob>();
        services.AddScoped<IClickEnrichmentScheduler, HangfireClickEnrichmentScheduler>();

        services.AddHttpClient<IIpGeolocationService, IpApiGeolocationService>(client =>
        {
            client.BaseAddress = new Uri("http://ip-api.com/");
            client.Timeout = TimeSpan.FromSeconds(5);
        })
        // Standard pipeline: retry on transient failures, circuit breaker on
        // sustained failure, per-attempt and total timeouts. All defaults.
        .AddStandardResilienceHandler();

        return services;
    }
}
