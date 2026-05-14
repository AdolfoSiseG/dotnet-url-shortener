using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using UrlShortener.Api.Endpoints;
using UrlShortener.Application;
using UrlShortener.Application.Auth;
using UrlShortener.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Hangfire wiring lives in the composition root because AddHangfire and
// AddHangfireServer ship in Hangfire.AspNetCore, and Infrastructure is a
// plain classlib that should not pull in ASP.NET Core. The Hangfire:Enabled
// flag lets integration tests skip the worker and storage initialization,
// since they replace IClickEnrichmentScheduler with a no-op.
var hangfireEnabled = builder.Configuration.GetValue("Hangfire:Enabled", true);

if (hangfireEnabled)
{
    var hangfireConn = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(hangfireConn),
            new PostgreSqlStorageOptions { SchemaName = "hangfire" }));
    builder.Services.AddHangfireServer();
}

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
if (jwtSettings is null
    || string.IsNullOrWhiteSpace(jwtSettings.Secret)
    || jwtSettings.Secret.Length < 32
    || string.IsNullOrWhiteSpace(jwtSettings.Issuer)
    || string.IsNullOrWhiteSpace(jwtSettings.Audience))
{
    throw new InvalidOperationException(
        "Jwt configuration is missing or invalid. Required: Jwt:Secret (>=32 chars), Jwt:Issuer, Jwt:Audience.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            // Default skew is 5 minutes; tighten to 30s for a portfolio-grade
            // demo where clock drift between issuer and validator is minimal.
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Hangfire dashboard is dev-only by default. Production deployments
    // should either skip it or gate it behind an admin claim — see week 11.
    // The package's default authorization filter already restricts access to
    // local requests, which is enough for a developer workstation.
    if (hangfireEnabled)
    {
        app.UseHangfireDashboard("/hangfire");
    }
}

// HTTPS redirect is only meaningful in Production where a real TLS
// certificate is configured. Skipping it in other environments avoids the
// "Failed to determine the https port for redirect" warning under the http
// launch profile and inside the integration test harness.
if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRedirect();

app.Run();

// Exposes the implicit Program type to the integration test assembly so
// WebApplicationFactory<Program> can host this app in-process.
public partial class Program { }
