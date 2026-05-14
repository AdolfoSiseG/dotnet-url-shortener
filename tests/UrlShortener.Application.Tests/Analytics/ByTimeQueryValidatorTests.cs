using FluentAssertions;
using UrlShortener.Application.Analytics.Dtos;
using UrlShortener.Application.Analytics.Validators;

namespace UrlShortener.Application.Tests.Analytics;

public class ByTimeQueryValidatorTests
{
    private readonly ByTimeQueryValidator _validator = new();

    [Fact]
    public void Accepts_a_well_formed_daily_query()
    {
        var query = new ByTimeQuery(
            From: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            To: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            Granularity: "day");

        _validator.Validate(query).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_when_from_is_after_to()
    {
        var query = new ByTimeQuery(
            From: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            To: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        _validator.Validate(query).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_an_unsupported_granularity()
    {
        var query = new ByTimeQuery(
            From: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            To: new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            Granularity: "hour");

        _validator.Validate(query).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("day")]
    [InlineData("week")]
    [InlineData("month")]
    [InlineData("DAY")]  // case-insensitive match
    public void Accepts_all_supported_granularities(string granularity)
    {
        var query = new ByTimeQuery(
            From: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            To: new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            Granularity: granularity);

        _validator.Validate(query).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_a_range_longer_than_one_year()
    {
        var query = new ByTimeQuery(
            From: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            To: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        _validator.Validate(query).IsValid.Should().BeFalse();
    }
}
