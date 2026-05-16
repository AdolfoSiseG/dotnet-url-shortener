using Npgsql;

namespace UrlShortener.Infrastructure.Persistence;

// Managed Postgres providers (Railway, Heroku, Render) expose the database
// as a single postgres:// URI. Npgsql needs key=value form, so translate a
// URI here. A value already in key=value form is returned unchanged, so
// local development keeps using its appsettings connection string.
public static class NpgsqlConnectionString
{
    public static string FromRaw(string raw)
    {
        raw = raw.Trim();

        if (!raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return raw;
        }

        // Parsed by hand rather than with System.Uri: provider-generated
        // passwords routinely contain '/', '+' or '=' which break Uri's
        // authority parsing and silently yield an empty host.
        var afterScheme = raw[(raw.IndexOf("://", StringComparison.Ordinal) + 3)..];

        var at = afterScheme.LastIndexOf('@');
        if (at < 0)
        {
            throw new InvalidOperationException(
                "Database URL has no '@' separating credentials from host. " +
                "Check the connection string / Railway variable reference.");
        }

        var credentials = afterScheme[..at];
        var hostAndDb = afterScheme[(at + 1)..];

        var credColon = credentials.IndexOf(':');
        var user = credColon >= 0 ? credentials[..credColon] : credentials;
        var password = credColon >= 0 ? credentials[(credColon + 1)..] : string.Empty;

        var slash = hostAndDb.IndexOf('/');
        var hostPort = slash >= 0 ? hostAndDb[..slash] : hostAndDb;
        var dbAndQuery = slash >= 0 ? hostAndDb[(slash + 1)..] : string.Empty;

        var question = dbAndQuery.IndexOf('?');
        var database = question >= 0 ? dbAndQuery[..question] : dbAndQuery;

        var portColon = hostPort.LastIndexOf(':');
        var host = portColon >= 0 ? hostPort[..portColon] : hostPort;
        var port = portColon >= 0 && int.TryParse(hostPort[(portColon + 1)..], out var parsed)
            ? parsed
            : 5432;

        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException(
                "Database URL resolved without a host. The Railway variable " +
                "reference is likely empty — use DATABASE_PUBLIC_URL.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            Username = Uri.UnescapeDataString(user),
            Password = Uri.UnescapeDataString(password),
            Database = database,
            // Managed Postgres requires TLS; SslMode.Require encrypts without
            // validating the provider-internal CA (Npgsql 8+ needs no
            // TrustServerCertificate for this).
            SslMode = SslMode.Require
        };

        return builder.ConnectionString;
    }
}
