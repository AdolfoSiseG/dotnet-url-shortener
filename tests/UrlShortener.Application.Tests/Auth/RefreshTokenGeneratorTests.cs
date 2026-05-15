using FluentAssertions;
using UrlShortener.Infrastructure.Auth;

namespace UrlShortener.Application.Tests.Auth;

public class RefreshTokenGeneratorTests
{
    private readonly RefreshTokenGenerator _generator = new();

    [Fact]
    public void Generate_returns_a_non_empty_token_and_hash()
    {
        var result = _generator.Generate();

        result.Token.Should().NotBeNullOrEmpty();
        result.Hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Generate_produces_different_tokens_each_call()
    {
        var a = _generator.Generate();
        var b = _generator.Generate();

        // 64-byte random tokens have 512 bits of entropy; collisions are
        // astronomically unlikely.
        a.Token.Should().NotBe(b.Token);
        a.Hash.Should().NotBe(b.Hash);
    }

    [Fact]
    public void Generate_returned_hash_matches_independent_compute()
    {
        var generated = _generator.Generate();

        var recomputed = _generator.ComputeHash(generated.Token);

        recomputed.Should().Be(generated.Hash);
    }

    [Fact]
    public void ComputeHash_is_deterministic()
    {
        const string input = "any-token-string";

        var hash1 = _generator.ComputeHash(input);
        var hash2 = _generator.ComputeHash(input);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeHash_returns_64_uppercase_hex_chars()
    {
        var hash = _generator.ComputeHash("anything");

        hash.Should().HaveLength(64);  // SHA-256 = 32 bytes = 64 hex chars
        hash.Should().MatchRegex("^[0-9A-F]+$");
    }
}
