using FluentAssertions;
using UrlShortener.Infrastructure.QrCodes;

namespace UrlShortener.Application.Tests.QrCodes;

public class QrCodeGeneratorTests
{
    private readonly QrCodeGenerator _generator = new();

    [Fact]
    public void GenerateAsPng_returns_a_non_empty_buffer()
    {
        var bytes = _generator.GenerateAsPng("https://example.com/abc1234");

        bytes.Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateAsPng_returns_a_buffer_with_the_png_magic_header()
    {
        var bytes = _generator.GenerateAsPng("https://example.com/abc1234");

        // PNG files always start with the 8-byte signature 89 50 4E 47 0D 0A 1A 0A.
        bytes.Length.Should().BeGreaterThan(8);
        bytes[0..4].Should().Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
    }

    [Fact]
    public void GenerateAsPng_produces_larger_output_for_higher_pixels_per_module()
    {
        var small = _generator.GenerateAsPng("https://example.com/abc1234", pixelsPerModule: 4);
        var large = _generator.GenerateAsPng("https://example.com/abc1234", pixelsPerModule: 20);

        large.Length.Should().BeGreaterThan(small.Length);
    }
}
