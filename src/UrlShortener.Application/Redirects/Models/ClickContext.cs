namespace UrlShortener.Application.Redirects.Models;

// Built by the API layer from HttpContext and passed to the service so the
// service stays free of ASP.NET Core types and can be unit-tested without
// spinning up a request pipeline.
public record ClickContext(string IpAddress, string UserAgent, string? Referrer);
