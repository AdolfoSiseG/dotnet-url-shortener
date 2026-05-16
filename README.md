# URL Shortener with Analytics

[![CI](https://github.com/AdolfoSiseG/dotnet-url-shortener/actions/workflows/ci.yml/badge.svg)](https://github.com/AdolfoSiseG/dotnet-url-shortener/actions/workflows/ci.yml)
![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-4169E1?logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-ready-2496ED?logo=docker&logoColor=white)
![License](https://img.shields.io/badge/license-MIT-green)

A URL shortener with detailed click analytics, built in .NET 10 with Clean
Architecture, JWT auth, background click enrichment, and a deployed demo.

## Live demo

- **API + interactive docs:** https://dotnet-url-shortener-production.up.railway.app
- The root URL opens the Scalar API explorer. Register a user, authorize
  with the returned token, and exercise every endpoint from the browser.

## Features

- Shorten URLs; fast public 301 redirects.
- Click capture with asynchronous enrichment (geolocation, device, browser, bot detection) via Hangfire.
- Analytics endpoints: per-link, overview, by country, by device, time series, by referrer.
- Optional password protection and expiry on individual links.
- QR code generation per link (PNG).
- JWT authentication with rotating refresh tokens.
- Per-IP rate limiting on public and auth endpoints; RFC 7807 Problem Details errors.
- OpenAPI document with an interactive Scalar UI.

## Tech stack

.NET 10 · ASP.NET Core · Entity Framework Core 10 · PostgreSQL 17 · Hangfire · xUnit + Moq + FluentAssertions · Docker · GitHub Actions

## Architecture

Clean Architecture, four layers:

```
src/
  UrlShortener.Domain          Entities; no external dependencies
  UrlShortener.Application     DTOs, services, interfaces, validators
  UrlShortener.Infrastructure  EF Core, external integrations, background jobs
  UrlShortener.Api             Controllers, middleware, composition root
tests/
  UrlShortener.Application.Tests     Unit tests (services, validators, jobs)
  UrlShortener.Api.IntegrationTests  End-to-end via WebApplicationFactory + Testcontainers
```

Dependency direction: `Api → Application + Infrastructure`, `Infrastructure → Application`, `Application → Domain`.

## Run with Docker Compose

The fastest way to get the full stack (API + PostgreSQL) running:

```bash
cp .env.example .env
# edit .env and set JWT_SECRET to a random string of 32+ characters
docker compose up --build
```

API on `http://localhost:8080`, interactive docs at
`http://localhost:8080/scalar/v1`. Compose applies EF migrations on startup.

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
| `ConnectionStrings__Default` | PostgreSQL connection string (key=value or a `postgres://` URI) |
| `Jwt__Secret` | JWT signing key, 32+ chars (never commit the real value) |
| `Jwt__Issuer` / `Jwt__Audience` | JWT issuer and audience |
| `Database__MigrateOnStartup` | `true` applies EF migrations on boot (used by Compose and the demo) |

## Tests

```bash
dotnet test
```

99 tests: unit tests for services, validators and the enrichment job, plus
integration tests that run against a real PostgreSQL via Testcontainers and
cover auth, link ownership, redirect status branches, rate limiting and the
Problem Details contract. CI runs the full suite on every push.

## Deploy (Railway)

Railway builds the repo's `Dockerfile` directly:

1. New Railway project from the GitHub repo.
2. Add a PostgreSQL plugin.
3. Set service variables: `ConnectionStrings__Default` (paste the Postgres
   `DATABASE_PUBLIC_URL` value), `Jwt__Secret`, `Jwt__Issuer`,
   `Jwt__Audience`, `Database__MigrateOnStartup=true`,
   `ASPNETCORE_ENVIRONMENT=Production`.
4. Generate a domain under Settings → Networking.

Migrations are applied on startup behind the `Database__MigrateOnStartup`
flag for simplicity. A production-grade deployment would run them as a
separate release step so a bad migration cannot break the app on boot.

## License

MIT — see [LICENSE](LICENSE).

## Author

**Adolfo Sise** — [GitHub](https://github.com/AdolfoSiseG) · [LinkedIn](https://linkedin.com/in/adolfo-s-ba-sv)
