namespace UrlShortener.Application.Auth.Dtos;

public record UserDto(Guid Id, string Email, DateTime CreatedAt);
