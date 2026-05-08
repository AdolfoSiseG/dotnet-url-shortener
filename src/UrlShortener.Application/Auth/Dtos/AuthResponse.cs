namespace UrlShortener.Application.Auth.Dtos;

public record AuthResponse(string AccessToken, DateTime ExpiresAt, UserDto User);
