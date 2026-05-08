using FluentAssertions;
using Moq;
using UrlShortener.Application.Auth.Interfaces;
using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Application.Links.Dtos;
using UrlShortener.Application.Links.Interfaces;
using UrlShortener.Application.Links.Services;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain.Exceptions;

namespace UrlShortener.Application.Tests.Links;

public class ShortLinkServiceTests
{
    private readonly Mock<IShortLinkRepository> _links = new();
    private readonly Mock<IShortCodeGenerator> _codeGen = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private ShortLinkService BuildService() =>
        new(_links.Object, _codeGen.Object, _hasher.Object, _uow.Object);

    [Fact]
    public async Task CreateAsync_uses_generator_when_no_custom_slug_is_given()
    {
        _codeGen.Setup(g => g.Generate(It.IsAny<int>())).Returns("abc1234");
        _links.Setup(l => l.ExistsByShortCodeAsync("abc1234", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var dto = await BuildService().CreateAsync(
            Guid.NewGuid(),
            new CreateShortLinkRequest("https://example.com"));

        dto.ShortCode.Should().Be("abc1234");
        _links.Verify(l => l.AddAsync(It.IsAny<ShortLink>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_retries_on_short_code_collision()
    {
        _codeGen.SetupSequence(g => g.Generate(It.IsAny<int>()))
            .Returns("collide")
            .Returns("unique1");
        _links.Setup(l => l.ExistsByShortCodeAsync("collide", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _links.Setup(l => l.ExistsByShortCodeAsync("unique1", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var dto = await BuildService().CreateAsync(
            Guid.NewGuid(),
            new CreateShortLinkRequest("https://example.com"));

        dto.ShortCode.Should().Be("unique1");
        _codeGen.Verify(g => g.Generate(It.IsAny<int>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateAsync_throws_when_collision_retries_are_exhausted()
    {
        _codeGen.Setup(g => g.Generate(It.IsAny<int>())).Returns("collide");
        _links.Setup(l => l.ExistsByShortCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => BuildService().CreateAsync(
            Guid.NewGuid(),
            new CreateShortLinkRequest("https://example.com"));

        await act.Should().ThrowAsync<ShortCodeGenerationException>();
    }

    [Fact]
    public async Task CreateAsync_uses_custom_slug_when_provided()
    {
        _links.Setup(l => l.ExistsByShortCodeAsync("my-slug", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var dto = await BuildService().CreateAsync(
            Guid.NewGuid(),
            new CreateShortLinkRequest("https://example.com", CustomSlug: "my-slug"));

        dto.ShortCode.Should().Be("my-slug");
        _codeGen.Verify(g => g.Generate(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_throws_when_custom_slug_is_taken()
    {
        _links.Setup(l => l.ExistsByShortCodeAsync("taken", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => BuildService().CreateAsync(
            Guid.NewGuid(),
            new CreateShortLinkRequest("https://example.com", CustomSlug: "taken"));

        await act.Should().ThrowAsync<ShortCodeAlreadyTakenException>();
    }

    [Fact]
    public async Task DeleteAsync_marks_the_link_as_soft_deleted_when_found()
    {
        var userId = Guid.NewGuid();
        var link = new ShortLink
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ShortCode = "abc1234",
            OriginalUrl = "https://example.com",
            CreatedAt = DateTime.UtcNow
        };
        _links.Setup(l => l.FindByIdAsync(userId, link.Id, It.IsAny<CancellationToken>())).ReturnsAsync(link);

        var deleted = await BuildService().DeleteAsync(userId, link.Id);

        deleted.Should().BeTrue();
        link.DeletedAt.Should().NotBeNull();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_returns_false_when_link_is_not_found()
    {
        var userId = Guid.NewGuid();
        _links.Setup(l => l.FindByIdAsync(userId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((ShortLink?)null);

        var deleted = await BuildService().DeleteAsync(userId, Guid.NewGuid());

        deleted.Should().BeFalse();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
