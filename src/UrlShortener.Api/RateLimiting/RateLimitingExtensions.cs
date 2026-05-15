using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace UrlShortener.Api.RateLimiting;

// Centralizes the rate-limiting policies so Program.cs stays a thin
// composition root. State lives in the limiter's in-memory partitions —
// per-process. A multi-instance deploy needs a distributed limiter
// (e.g. Redis-backed) for a coherent per-IP budget across replicas.
public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";
    public const string RedirectPolicy = "redirect";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            // Fixed window: hard cap for sensitive endpoints. A brute-force
            // attempt against /login is cleanly cut off and resets per window.
            options.AddPolicy(AuthPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ClientIpResolver.Resolve(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));

            // Token bucket: tolerates legitimate bursts (a dashboard opening
            // ten links at once) while still throttling sustained spam.
            options.AddPolicy(RedirectPolicy, context =>
                RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: ClientIpResolver.Resolve(context),
                    factory: _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 60,
                        TokensPerPeriod = 10,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                        AutoReplenishment = true,
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Custom rejection writes RFC 7807 Problem Details so the 429
            // response shape matches the rest of the API's error contract.
            options.OnRejected = async (context, cancellationToken) =>
            {
                var retryAfterSeconds =
                    context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                        ? (int)Math.Ceiling(retryAfter.TotalSeconds)
                        : 60;

                context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();

                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too Many Requests",
                    Detail = $"Rate limit exceeded. Try again in {retryAfterSeconds} seconds.",
                    Type = "https://httpstatuses.io/429",
                    Instance = context.HttpContext.Request.Path
                };

                // The content-type overload of WriteAsJsonAsync is what
                // pins the response to application/problem+json — setting
                // Response.ContentType earlier gets overwritten otherwise.
                await context.HttpContext.Response.WriteAsJsonAsync(
                    problem, options: null, contentType: "application/problem+json", cancellationToken);
            };
        });
}
