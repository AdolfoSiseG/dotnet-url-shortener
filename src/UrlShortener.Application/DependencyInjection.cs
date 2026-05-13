using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Application.Auth.Interfaces;
using UrlShortener.Application.Auth.Services;
using UrlShortener.Application.Links.Interfaces;
using UrlShortener.Application.Links.Services;
using UrlShortener.Application.Redirects.Interfaces;
using UrlShortener.Application.Redirects.Services;

namespace UrlShortener.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IShortLinkService, ShortLinkService>();
        services.AddScoped<IRedirectService, RedirectService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
