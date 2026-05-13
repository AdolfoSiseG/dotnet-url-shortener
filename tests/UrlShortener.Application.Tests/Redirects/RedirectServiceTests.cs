using FluentAssertions;
using Moq;
using UrlShortener.Application.Auth.Interfaces;
using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Application.Redirects.Models;
using UrlShortener.Application.Redirects.Services;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Tests.Redirects;

public class RedirectServiceTests
{
    private readonly Mock<IShortLinkRepository> _links = new();
    private readonly Mock<IClickRepository> _clicks = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IClickEnrichmentScheduler> _scheduler = new();

    private RedirectService BuildService() =>
        new(_links.Object, _clicks.Object, _hasher.Object, _uow.Object, _scheduler.Object);

    private static ClickContext SampleContext() => new("127.0.0.1", "test-agent", null);

    private static ShortLink BuildLink(
        bool isActive = true,
        DateTime? expiresAt = null,
        string? passwordHash = null) => new()
        {
            Id = Guid.NewGuid(),
            ShortCode = "abc1234",
            OriginalUrl = "https://example.com",
            IsActive = isActive,
            ExpiresAt = expiresAt,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

    [Fact]
    public async Task ResolveAsync_returns_RedirectFound_for_an_active_link()
    {
        var link = BuildLink();
        _links.Setup(l => l.FindByShortCodeAsync("abc1234", It.IsAny<CancellationToken>())).ReturnsAsync(link);

        var result = await BuildService().ResolveAsync("abc1234", SampleContext());

        result.Should().BeOfType<RedirectFound>()
            .Which.TargetUrl.Should().Be("https://example.com");
    }

    [Fact]
    public async Task ResolveAsync_returns_RedirectNotFound_for_an_unknown_code()
    {
        _links.Setup(l => l.FindByShortCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ShortLink?)null);

        var result = await BuildService().ResolveAsync("nothere", SampleContext());

        result.Should().BeOfType<RedirectNotFound>();
        _clicks.Verify(c => c.AddAsync(It.IsAny<Click>(), It.IsAny<CancellationToken>()), Times.Never);
        _scheduler.Verify(s => s.Schedule(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_returns_RedirectGone_for_an_inactive_link()
    {
        _links.Setup(l => l.FindByShortCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLink(isActive: false));

        var result = await BuildService().ResolveAsync("abc1234", SampleContext());

        result.Should().BeOfType<RedirectGone>();
        _clicks.Verify(c => c.AddAsync(It.IsAny<Click>(), It.IsAny<CancellationToken>()), Times.Never);
        _scheduler.Verify(s => s.Schedule(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_returns_RedirectGone_for_an_expired_link()
    {
        _links.Setup(l => l.FindByShortCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLink(expiresAt: DateTime.UtcNow.AddMinutes(-1)));

        var result = await BuildService().ResolveAsync("abc1234", SampleContext());

        result.Should().BeOfType<RedirectGone>();
    }

    [Fact]
    public async Task ResolveAsync_returns_PasswordRequired_for_a_protected_link()
    {
        _links.Setup(l => l.FindByShortCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLink(passwordHash: "hash"));

        var result = await BuildService().ResolveAsync("abc1234", SampleContext());

        result.Should().BeOfType<RedirectPasswordRequired>()
            .Which.LastAttemptFailed.Should().BeFalse();
        _clicks.Verify(c => c.AddAsync(It.IsAny<Click>(), It.IsAny<CancellationToken>()), Times.Never);
        _scheduler.Verify(s => s.Schedule(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_captures_a_click_on_successful_redirect()
    {
        var link = BuildLink();
        _links.Setup(l => l.FindByShortCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(link);

        await BuildService().ResolveAsync("abc1234", new ClickContext("1.2.3.4", "Chrome", "https://x.com"));

        _clicks.Verify(c => c.AddAsync(
            It.Is<Click>(click =>
                click.ShortLinkId == link.Id
                && click.IpAddress == "1.2.3.4"
                && click.UserAgent == "Chrome"
                && click.Referrer == "https://x.com"),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_schedules_enrichment_after_recording_a_click()
    {
        var link = BuildLink();
        _links.Setup(l => l.FindByShortCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(link);

        await BuildService().ResolveAsync("abc1234", SampleContext());

        // Enrichment must be scheduled exactly once per successful redirect.
        _scheduler.Verify(s => s.Schedule(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task UnlockAsync_returns_RedirectFound_with_correct_password()
    {
        var link = BuildLink(passwordHash: "stored-hash");
        _links.Setup(l => l.FindByShortCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(link);
        _hasher.Setup(h => h.Verify("right", "stored-hash")).Returns(true);

        var result = await BuildService().UnlockAsync("abc1234", "right", SampleContext());

        result.Should().BeOfType<RedirectFound>()
            .Which.TargetUrl.Should().Be("https://example.com");
        _clicks.Verify(c => c.AddAsync(It.IsAny<Click>(), It.IsAny<CancellationToken>()), Times.Once);
        _scheduler.Verify(s => s.Schedule(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task UnlockAsync_returns_PasswordRequired_with_failed_flag_on_wrong_password()
    {
        var link = BuildLink(passwordHash: "stored-hash");
        _links.Setup(l => l.FindByShortCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(link);
        _hasher.Setup(h => h.Verify("wrong", "stored-hash")).Returns(false);

        var result = await BuildService().UnlockAsync("abc1234", "wrong", SampleContext());

        result.Should().BeOfType<RedirectPasswordRequired>()
            .Which.LastAttemptFailed.Should().BeTrue();
        _clicks.Verify(c => c.AddAsync(It.IsAny<Click>(), It.IsAny<CancellationToken>()), Times.Never);
        _scheduler.Verify(s => s.Schedule(It.IsAny<Guid>()), Times.Never);
    }
}
