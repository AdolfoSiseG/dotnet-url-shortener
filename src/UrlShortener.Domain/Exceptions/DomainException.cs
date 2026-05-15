namespace UrlShortener.Domain.Exceptions;

// Base class for exceptions that represent domain-level invariant violations.
// Caught by global error middleware (week 10) and translated to HTTP responses.
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}
