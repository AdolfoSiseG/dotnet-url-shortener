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
- Rate limiting on public and auth endpoints; RFC 7807 error responses.
- OpenAPI documentation with an interactive Scalar UI.

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

## Run with Docker Compose

The fastest way to get the full stack (API + PostgreSQL) running:

```bash
cp .env.example .env
# edit .env and set JWT_SECRET to a random string of 32+ characters
docker compose up --build
```

The API is then on `http://localhost:8080`. Interactive API docs (Scalar)
are at `http://localhost:8080/scalar/v1`. Compose applies EF migrations on
startup automatically.

## Run locally for development

Prerequisites: .NET 10 SDK, Docker (for PostgreSQL).

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

# Run the API (http://localhost:5282)
dotnet run --project src/UrlShortener.Api
```

## Configuration

All runtime configuration is supplied via environment variables; see
[`.env.example`](.env.example) for the full list. Required:

| Variable | Purpose |
|---|---|
| `ConnectionStrings__Default` | PostgreSQL connection string |
| `Jwt__Secret` | JWT signing key, 32+ chars (never commit the real value) |
| `Jwt__Issuer` / `Jwt__Audience` | JWT issuer and audience |
| `Database__MigrateOnStartup` | `true` applies EF migrations on boot (used by Compose) |

## Deploy (Railway)

Railway builds the repo's `Dockerfile` directly:

1. Create a new Railway project from the GitHub repo.
2. Add a PostgreSQL plugin; Railway injects its connection variables.
3. Set the service variables: `ConnectionStrings__Default` (from the
   Postgres plugin), `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience`, and
   `Database__MigrateOnStartup=true`.
4. Deploy. Railway detects the `Dockerfile`, builds, and exposes the
   service.

Migrations: this project applies them on startup behind the
`Database__MigrateOnStartup` flag for simplicity. A production-grade
deployment would instead run migrations as a separate release step so a
bad migration cannot break the app on boot.

## License

MIT — see [LICENSE](LICENSE).

## Author

**Adolfo Sise** — [GitHub](https://github.com/AdolfoSiseG) · [LinkedIn](https://linkedin.com/in/adolfo-s-ba-sv)
