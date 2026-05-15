using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Common.Interfaces;

public interface IRefreshTokenRepository
{
    // Returns the token with its owning User eagerly loaded, since every
    // refresh-flow caller needs the User to issue a new access token.
    Task<RefreshToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
}
