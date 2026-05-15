# URL Shortener with Analytics

[![CI](https://github.com/AdolfoSiseG/dotnet-url-shortener/actions/workflows/ci.yml/badge.svg)](https://github.com/AdolfoSiseG/dotnet-url-shortener/actions/workflows/ci.yml)

A URL shortener with detailed click analytics, built in .NET 10.

> **Status:** Work in progress. v1.0 in active development.

## Target features (v1.0)

- Shorten URLs; fast public 301 redirects.
- Click capture with asynchronous enrichment (geolocation, device, browser, bot detection).
- Analytics endpoints: per-link, by country, by device, time series, by referrer.
- Optional password protection and expiry on individual links.
- QR code generation per link.
- JWT authentication with refresh tokens.
- OpenAPI / Swagger documentation.

## Tech stack

.NET 10 · ASP.NET Core · Entity Framework Core 10 · PostgreSQL 17 · Hangfire · xUnit + Moq + FluentAssertions · Docker

## Architecture

Clean Architecture, four layers:

```
src/
  UrlShortener.Domain          Entities; no external dependencies
  UrlShortener.Application     DTOs, services, interfaces, validators
  UrlShortener.Infrastructure  EF Core, external integrations, background jobs
  UrlShortener.Api             Controllers, middleware, composition root
tests/
  UrlShortener.Application.Tests
  UrlShortener.Api.IntegrationTests
```

Dependency direction: `Api → Application + Infrastructure`, `Infrastructure → Application`, `Application → Domain`.

## Local setup

Prerequisites: .NET 10 SDK, Docker.

```bash
# Start Postgres
docker run --name urlshortener-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=urlshortener \
  -p 5432:5432 -d postgres:17

# Apply migrations
dotnet ef database update \
  --project src/UrlShortener.Infrastructure \
  --startup-project src/UrlShortener.Api

# Run the API
dotnet run --project src/UrlShortener.Api
```

## License

MIT — see [LICENSE](LICENSE).

## Author

**Adolfo Sise** — [GitHub](https://github.com/AdolfoSiseG) · [LinkedIn](https://linkedin.com/in/adolfo-s-ba-sv)
