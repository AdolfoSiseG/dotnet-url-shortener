using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Application.Common.Models;
using UrlShortener.Domain.Entities;
using UrlShortener.Infrastructure.Jobs;

namespace UrlShortener.Application.Tests.Enrichment;

public class ClickEnrichmentJobTests
{
    private readonly Mock<IClickRepository> _clicks = new();
    private readonly Mock<IUserAgentParser> _ua = new();
    private readonly Mock<IIpGeolocationService> _geo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private ClickEnrichmentJob BuildJob() =>
        new(_clicks.Object, _ua.Object, _geo.Object, _uow.Object, NullLogger<ClickEnrichmentJob>.Instance);

    private static Click BuildClick() => new()
    {
        Id = Guid.NewGuid(),
        ShortLinkId = Guid.NewGuid(),
        ClickedAt = DateTime.UtcNow,
        IpAddress = "8.8.8.8",
        UserAgent = "Mozilla/5.0 (Macintosh) Chrome/124"
    };

    [Fact]
    public async Task RunAsync_does_nothing_when_click_is_not_found()
    {
        _clicks.Setup(c => c.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Click?)null);

        await BuildJob().RunAsync(Guid.NewGuid());

        _ua.Verify(p => p.Parse(It.IsAny<string>()), Times.Never);
        _geo.Verify(g => g.LookupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_fills_browser_os_device_from_user_agent()
    {
        var click = BuildClick();
        _clicks.Setup(c => c.FindByIdAsync(click.Id, It.IsAny<CancellationToken>())).ReturnsAsync(click);
        _ua.Setup(p => p.Parse(click.UserAgent)).Returns(new UserAgentInfo("Chrome", "Mac OS X", "desktop", IsBot: false));
        _geo.Setup(g => g.LookupAsync(click.IpAddress, It.IsAny<CancellationToken>())).ReturnsAsync(new GeoLookupResult(null, null));

        await BuildJob().RunAsync(click.Id);

        click.Browser.Should().Be("Chrome");
        click.Os.Should().Be("Mac OS X");
        click.Device.Should().Be("desktop");
        click.IsBot.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_fills_country_and_city_when_geo_lookup_returns_data()
    {
        var click = BuildClick();
        _clicks.Setup(c => c.FindByIdAsync(click.Id, It.IsAny<CancellationToken>())).ReturnsAsync(click);
        _ua.Setup(p => p.Parse(click.UserAgent)).Returns(new UserAgentInfo("Chrome", "Mac OS X", "desktop", false));
        _geo.Setup(g => g.LookupAsync(click.IpAddress, It.IsAny<CancellationToken>())).ReturnsAsync(new GeoLookupResult("United States", "Mountain View"));

        await BuildJob().RunAsync(click.Id);

        click.Country.Should().Be("United States");
        click.City.Should().Be("Mountain View");
    }

    [Fact]
    public async Task RunAsync_leaves_geo_fields_null_when_lookup_returns_null()
    {
        var click = BuildClick();
        _clicks.Setup(c => c.FindByIdAsync(click.Id, It.IsAny<CancellationToken>())).ReturnsAsync(click);
        _ua.Setup(p => p.Parse(click.UserAgent)).Returns(new UserAgentInfo("Chrome", "Mac OS X", "desktop", false));
        _geo.Setup(g => g.LookupAsync(click.IpAddress, It.IsAny<CancellationToken>())).ReturnsAsync((GeoLookupResult?)null);

        await BuildJob().RunAsync(click.Id);

        click.Country.Should().BeNull();
        click.City.Should().BeNull();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_skips_geo_lookup_for_bots()
    {
        var click = BuildClick();
        _clicks.Setup(c => c.FindByIdAsync(click.Id, It.IsAny<CancellationToken>())).ReturnsAsync(click);
        _ua.Setup(p => p.Parse(click.UserAgent)).Returns(new UserAgentInfo("Googlebot", null, "bot", IsBot: true));

        await BuildJob().RunAsync(click.Id);

        click.IsBot.Should().BeTrue();
        click.Device.Should().Be("bot");
        _geo.Verify(g => g.LookupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
