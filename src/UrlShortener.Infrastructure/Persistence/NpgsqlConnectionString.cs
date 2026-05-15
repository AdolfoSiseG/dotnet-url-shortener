using Npgsql;

namespace UrlShortener.Infrastructure.Persistence;

// Managed Postgres providers (Railway, Heroku, Render) expose the database
// as a single postgres:// URI. Npgsql needs key=value form, so translate a
// URI once here. A value already in key=value form is returned unchanged,
// so local development keeps using its appsettings connection string.
public static class NpgsqlConnectionString
{
    public static string FromRaw(string raw)
    {
        if (!raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return raw;
        }

        var uri = new Uri(raw);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            Database = uri.AbsolutePath.Trim('/'),
            // Managed Postgres requires TLS. SslMode.Require encrypts without
            // validating the provider-internal CA, which is what Railway and
            // similar hosts need (Npgsql 8+ no longer needs
            // TrustServerCertificate for this).
            SslMode = SslMode.Require
        };

        return builder.ConnectionString;
    }
}
