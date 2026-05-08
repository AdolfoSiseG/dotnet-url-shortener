using FluentAssertions;
using UrlShortener.Infrastructure.Auth;

namespace UrlShortener.Application.Tests.Auth;

public class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_returns_a_value_different_from_the_input()
    {
        var hash = _hasher.Hash("MyPassword123");

        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe("MyPassword123");
    }

    [Fact]
    public void Hash_produces_unique_outputs_for_the_same_input()
    {
        var hash1 = _hasher.Hash("MyPassword123");
        var hash2 = _hasher.Hash("MyPassword123");

        // BCrypt embeds a unique random salt in every hash; two hashes of
        // the same password must never collide.
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Verify_returns_true_for_the_correct_password()
    {
        var hash = _hasher.Hash("MyPassword123");

        _hasher.Verify("MyPassword123", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_returns_false_for_the_wrong_password()
    {
        var hash = _hasher.Hash("MyPassword123");

        _hasher.Verify("WrongPassword", hash).Should().BeFalse();
    }
}
