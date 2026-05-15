namespace UrlShortener.Domain.Exceptions;

// Raised when the random generator produces colliding codes for too many
// attempts in a row. Indicates either an exhausted code space (we should
// increase length) or a degraded RNG.
public sealed class ShortCodeGenerationException()
    : DomainException("Could not generate a unique short code after several attempts.");
