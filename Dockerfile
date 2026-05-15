# syntax=docker/dockerfile:1

# --- build stage: full SDK, compiles and publishes the API ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project files first and restore. Docker caches this layer,
# so a source-only change does not re-trigger a full restore.
COPY src/UrlShortener.Domain/*.csproj src/UrlShortener.Domain/
COPY src/UrlShortener.Application/*.csproj src/UrlShortener.Application/
COPY src/UrlShortener.Infrastructure/*.csproj src/UrlShortener.Infrastructure/
COPY src/UrlShortener.Api/*.csproj src/UrlShortener.Api/
RUN dotnet restore src/UrlShortener.Api/UrlShortener.Api.csproj

# Copy the rest of the source and publish only the API project.
COPY src/ src/
RUN dotnet publish src/UrlShortener.Api/UrlShortener.Api.csproj \
    -c Release -o /app/publish --no-restore

# --- final stage: runtime only, no SDK or build tooling ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# .NET 8+ images ship a predefined non-root user; $APP_UID resolves to it.
# The app writes nothing to the filesystem (logs to stdout, data to
# Postgres), so no chown is needed.
USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "UrlShortener.Api.dll"]
