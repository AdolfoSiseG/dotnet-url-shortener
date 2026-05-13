namespace UrlShortener.Application.Common.Models;

// Device values: "mobile" | "tablet" | "desktop" | "bot" | "unknown".
// Browser and Os are null when the parser cannot identify them.
public record UserAgentInfo(string? Browser, string? Os, string Device, bool IsBot);
