using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Extensions;
using UrlShortener.Application.Analytics.Dtos;
using UrlShortener.Application.Analytics.Interfaces;
using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Application.Common.Models;
using UrlShortener.Application.Links.Dtos;
using UrlShortener.Application.Links.Interfaces;
using UrlShortener.Domain.Exceptions;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/links")]
public class ShortLinksController(
    IShortLinkService linksService,
    IAnalyticsService analytics,
    IQrCodeGenerator qrGenerator,
    IValidator<CreateShortLinkRequest> createValidator,
    IValidator<UpdateShortLinkRequest> updateValidator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ShortLinkDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ShortLinkDto>> Create(
        [FromBody] CreateShortLinkRequest request,
        CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return ValidationProblem(validation.ToString());

        try
        {
            var dto = await linksService.CreateAsync(User.GetUserId(), request, ct);
            return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
        }
        catch (ShortCodeAlreadyTakenException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<ShortLinkDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResult<ShortLinkDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        // Clamp paging server-side so a malicious client cannot request a
        // million-row page or a negative offset to pry at internals.
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = new ListShortLinksQuery(page, pageSize, status, search);
        var result = await linksService.ListAsync(User.GetUserId(), query, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ShortLinkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ShortLinkDto>> Get(Guid id, CancellationToken ct)
    {
        var dto = await linksService.GetAsync(User.GetUserId(), id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(ShortLinkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ShortLinkDto>> Update(
        Guid id,
        [FromBody] UpdateShortLinkRequest request,
        CancellationToken ct)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return ValidationProblem(validation.ToString());

        var dto = await linksService.UpdateAsync(User.GetUserId(), id, request, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await linksService.DeleteAsync(User.GetUserId(), id, ct);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/stats")]
    [ProducesResponseType(typeof(LinkStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LinkStatsDto>> Stats(Guid id, CancellationToken ct)
    {
        var stats = await analytics.GetLinkStatsAsync(User.GetUserId(), id, ct);
        return stats is null ? NotFound() : Ok(stats);
    }

    [HttpGet("{id:guid}/qr")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> QrCode(
        Guid id,
        [FromQuery] int size = 300,
        CancellationToken ct = default)
    {
        // Clamp to a sensible pixel range so a malicious client can't ask
        // the server to render a 100 000 × 100 000 PNG.
        size = Math.Clamp(size, 100, 1000);

        var link = await linksService.GetAsync(User.GetUserId(), id, ct);
        if (link is null) return NotFound();

        // QR matrices for short URLs land around 33 modules wide; convert
        // the user-facing pixel size into pixels-per-module, with a floor
        // that keeps very small requests still scannable.
        var pixelsPerModule = Math.Max(3, size / 33);

        // Compose the public short URL from the live request. Behind a
        // reverse proxy, Request.Scheme/Host reflect the original client
        // values once ForwardedHeadersMiddleware is configured (week 11).
        var shortUrl = $"{Request.Scheme}://{Request.Host}/{link.ShortCode}";
        var png = qrGenerator.GenerateAsPng(shortUrl, pixelsPerModule);

        return File(png, "image/png");
    }
}
