using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Domain.Exceptions;

namespace UrlShortener.Api.Middleware;

// Translates DomainException subtypes into RFC 7807 Problem Details
// responses. Anything not derived from DomainException returns false so
// the default ExceptionHandlerMiddleware emits the generic 500.
public class DomainExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment env,
    ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainException domain)
        {
            return false;
        }

        var (status, title) = exception switch
        {
            EmailAlreadyExistsException => (StatusCodes.Status409Conflict, "Conflict"),
            ShortCodeAlreadyTakenException => (StatusCodes.Status409Conflict, "Conflict"),
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            InvalidRefreshTokenException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ShortCodeGenerationException => (StatusCodes.Status503ServiceUnavailable, "Service Unavailable"),
            _ => (StatusCodes.Status400BadRequest, "Bad Request")
        };

        // Auth failures are common and noisy; log at Warning so they do not
        // pollute the error stream that real bugs need to stand out in.
        logger.LogWarning(domain, "Domain exception {Type} on {Method} {Path}",
            domain.GetType().Name, httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = status;

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = domain.Message,
            Type = $"https://httpstatuses.io/{status}",
            Instance = httpContext.Request.Path
        };

        // Stack traces are sensitive — only attach them in Development.
        if (env.IsDevelopment() && domain.StackTrace is not null)
        {
            problemDetails.Extensions["stackTrace"] = domain.StackTrace;
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = domain
        });
    }
}
