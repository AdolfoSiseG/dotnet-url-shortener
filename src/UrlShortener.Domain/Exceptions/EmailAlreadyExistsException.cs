namespace UrlShortener.Domain.Exceptions;

public sealed class EmailAlreadyExistsException(string email)
    : DomainException($"A user with email '{email}' already exists.");
