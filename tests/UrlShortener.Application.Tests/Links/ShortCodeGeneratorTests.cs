using FluentAssertions;
using UrlShortener.Infrastructure.ShortCodes;

namespace UrlShortener.Application.Tests.Links;

public class ShortCodeGeneratorTests
{
    private readonly ShortCodeGenerator _generator = new();

    [Fact]
    public void Generate_returns_a_string_of_default_length()
    {
        _generator.Generate().Should().HaveLength(7);
    }

    [Fact]
    public void Generate_respects_a_custom_length()
    {
        _generator.Generate(length: 10).Should().HaveLength(10);
    }

    [Fact]
    public void Generate_uses_only_base62_characters()
    {
        for (var i = 0; i < 100; i++)
        {
            _generator.Generate().Should().MatchRegex("^[0-9A-Za-z]+$");
        }
    }

    [Fact]
    public void Generate_produces_different_codes_across_calls()
    {
        // For length=7 with 62 alphabet, 100 random samples should hit
        // ~100 distinct values. Allow 5% slack for an unlucky run.
        var codes = Enumerable.Range(0, 100).Select(_ => _generator.Generate()).ToHashSet();
        codes.Count.Should().BeGreaterThan(95);
    }
}
