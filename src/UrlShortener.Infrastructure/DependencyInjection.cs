using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Application.Auth;
using UrlShortener.Application.Auth.Interfaces;
using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Infrastructure.Auth;
using UrlShortener.Infrastructure.Persistence;
using UrlShortener.Infrastructure.Persistence.Repositories;

namespace UrlShortener.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
