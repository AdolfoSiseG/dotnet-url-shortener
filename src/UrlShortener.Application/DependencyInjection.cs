using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Application.Auth.Interfaces;
using UrlShortener.Application.Auth.Services;

namespace UrlShortener.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
