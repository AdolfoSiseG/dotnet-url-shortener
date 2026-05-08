namespace UrlShortener.Domain.Exceptions;

// Intentionally generic message: do not signal whether the email or the
// password was wrong. Mitigates account-enumeration attacks on /login.
public sealed class InvalidCredentialsException()
    : DomainException("Email or password is incorrect.");
