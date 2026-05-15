namespace UrlShortener.Domain.Exceptions;

// Generic message: do not signal whether the token is unknown, expired,
// or already revoked. Same enumeration-mitigation rationale as login.
public sealed class InvalidRefreshTokenException()
    : DomainException("Refresh token is invalid or has expired.");
