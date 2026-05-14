using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Extensions;
using UrlShortener.Application.Analytics.Dtos;
using UrlShortener.Application.Analytics.Interfaces;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/stats")]
public class StatsController(
    IAnalyticsService analytics,
    IValidator<ByTimeQuery> byTimeValidator) : ControllerBase
{
    [HttpGet("overview")]
    [ProducesResponseType(typeof(OverviewStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<OverviewStatsDto>> Overview(CancellationToken ct) =>
        Ok(await analytics.GetOverviewAsync(User.GetUserId(), ct));

    [HttpGet("by-country")]
    [ProducesResponseType(typeof(IReadOnlyList<CountryStatDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<CountryStatDto>>> ByCountry(CancellationToken ct) =>
        Ok(await analytics.GetByCountryAsync(User.GetUserId(), ct));

    [HttpGet("by-device")]
    [ProducesResponseType(typeof(IReadOnlyList<DeviceStatDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<DeviceStatDto>>> ByDevice(CancellationToken ct) =>
        Ok(await analytics.GetByDeviceAsync(User.GetUserId(), ct));

    [HttpGet("by-time")]
    [ProducesResponseType(typeof(IReadOnlyList<TimeBucketStatDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<TimeBucketStatDto>>> ByTime(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string granularity = "day",
        CancellationToken ct = default)
    {
        var query = new ByTimeQuery(from, to, granularity);
        var validation = await byTimeValidator.ValidateAsync(query, ct);
        if (!validation.IsValid) return ValidationProblem(validation.ToString());

        return Ok(await analytics.GetByTimeAsync(User.GetUserId(), query, ct));
    }

    [HttpGet("by-referrer")]
    [ProducesResponseType(typeof(IReadOnlyList<ReferrerStatDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ReferrerStatDto>>> ByReferrer(CancellationToken ct) =>
        Ok(await analytics.GetByReferrerAsync(User.GetUserId(), ct));
}
