using UrlShortener.Application.Common.Models;

namespace UrlShortener.Application.Common.Interfaces;

public interface IUserAgentParser
{
    UserAgentInfo Parse(string userAgent);
}
