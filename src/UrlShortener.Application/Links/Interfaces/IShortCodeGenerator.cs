namespace UrlShortener.Application.Links.Interfaces;

public interface IShortCodeGenerator
{
    // Generates a random base62 string. Collision-checking against the DB
    // is the caller's responsibility (see ShortLinkService).
    string Generate(int length = 7);
}
