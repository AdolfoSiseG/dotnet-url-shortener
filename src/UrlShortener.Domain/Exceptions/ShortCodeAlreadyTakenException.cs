namespace UrlShortener.Domain.Exceptions;

public sealed class ShortCodeAlreadyTakenException(string shortCode)
    : DomainException($"Short code '{shortCode}' is already in use.");
