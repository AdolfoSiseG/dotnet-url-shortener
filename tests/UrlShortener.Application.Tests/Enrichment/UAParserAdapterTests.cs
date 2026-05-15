using FluentAssertions;
using UrlShortener.Infrastructure.UserAgents;

namespace UrlShortener.Application.Tests.Enrichment;

public class UAParserAdapterTests
{
    private readonly UAParserAdapter _parser = new();

    [Fact]
    public void Parse_returns_unknown_for_empty_user_agent()
    {
        var info = _parser.Parse(string.Empty);

        info.Browser.Should().BeNull();
        info.Os.Should().BeNull();
        info.Device.Should().Be("unknown");
        info.IsBot.Should().BeFalse();
    }

    [Fact]
    public void Parse_recognizes_chrome_on_desktop_windows()
    {
        const string ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";

        var info = _parser.Parse(ua);

        info.Browser.Should().Be("Chrome");
        info.Os.Should().Be("Windows");
        info.Device.Should().Be("desktop");
        info.IsBot.Should().BeFalse();
    }

    [Fact]
    public void Parse_classifies_iphone_safari_as_mobile()
    {
        const string ua = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Mobile/15E148 Safari/604.1";

        var info = _parser.Parse(ua);

        info.Os.Should().Be("iOS");
        info.Device.Should().Be("mobile");
        info.IsBot.Should().BeFalse();
    }

    [Fact]
    public void Parse_classifies_ipad_as_tablet()
    {
        const string ua = "Mozilla/5.0 (iPad; CPU OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Safari/604.1";

        var info = _parser.Parse(ua);

        info.Device.Should().Be("tablet");
        info.IsBot.Should().BeFalse();
    }

    [Fact]
    public void Parse_flags_googlebot_as_bot()
    {
        const string ua = "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)";

        var info = _parser.Parse(ua);

        info.IsBot.Should().BeTrue();
        info.Device.Should().Be("bot");
    }

    [Fact]
    public void Parse_flags_generic_crawler_keywords_as_bot()
    {
        const string ua = "MyCustomCrawler/1.0";

        var info = _parser.Parse(ua);

        info.IsBot.Should().BeTrue();
        info.Device.Should().Be("bot");
    }
}
