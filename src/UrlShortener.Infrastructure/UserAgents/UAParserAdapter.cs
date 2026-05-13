using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Application.Common.Models;

namespace UrlShortener.Infrastructure.UserAgents;

public class UAParserAdapter : IUserAgentParser
{
    private static readonly UAParser.Parser Parser = UAParser.Parser.GetDefault();

    // UAParser already marks well-known crawlers with Device.Family = "Spider",
    // but its rules lag behind new bots. The list below is a cheap belt-and-
    // braces check for common substrings that don't reach the Spider rules.
    private static readonly string[] BotKeywords =
        ["bot", "crawler", "spider", "scrap", "fetch", "preview", "monitoring", "headless"];

    public UserAgentInfo Parse(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return new UserAgentInfo(null, null, "unknown", IsBot: false);
        }

        var parsed = Parser.Parse(userAgent);
        var isBot = DetectBot(userAgent, parsed.Device.Family);
        var device = ClassifyDevice(isBot, parsed.Device.Family, userAgent);

        return new UserAgentInfo(
            Browser: parsed.UA.Family == "Other" ? null : parsed.UA.Family,
            Os: parsed.OS.Family == "Other" ? null : parsed.OS.Family,
            Device: device,
            IsBot: isBot);
    }

    private static bool DetectBot(string userAgent, string deviceFamily)
    {
        if (string.Equals(deviceFamily, "Spider", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return BotKeywords.Any(k => userAgent.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private static string ClassifyDevice(bool isBot, string deviceFamily, string rawUserAgent)
    {
        if (isBot) return "bot";

        // UAParser flags tablets through specific family names; iPad lands here too.
        if (deviceFamily.Contains("Tablet", StringComparison.OrdinalIgnoreCase)
            || deviceFamily.Equals("iPad", StringComparison.OrdinalIgnoreCase))
        {
            return "tablet";
        }

        // "Mobile" is a near-universal UA token on phones (incl. Android, iPhone).
        if (rawUserAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
        {
            return "mobile";
        }

        return "desktop";
    }
}
