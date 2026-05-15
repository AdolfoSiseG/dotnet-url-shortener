using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UrlShortener.Api.RateLimiting;
using UrlShortener.Application.Auth.Dtos;
using UrlShortener.Application.Auth.Interfaces;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
public class AuthController(
    IAuthService authService,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IValidator<RefreshRequest> refreshValidator,
    IValidator<LogoutRequest> logoutValidator) : ControllerBase
{
    /// <summary>Registers a new user and returns an access/refresh token pair.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken ct)
    {
        var validation = await registerValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return ValidationProblem(validation.ToString());

        return Ok(await authService.RegisterAsync(request, ct));
    }

    /// <summary>Authenticates a user and returns an access/refresh token pair.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var validation = await loginValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return ValidationProblem(validation.ToString());

        return Ok(await authService.LoginAsync(request, ct));
    }

    /// <summary>Rotates the supplied refresh token, returning a new pair.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken ct)
    {
        var validation = await refreshValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return ValidationProblem(validation.ToString());

        return Ok(await authService.RefreshAsync(request.RefreshToken, ct));
    }

    /// <summary>Revokes the supplied refresh token. Idempotent.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken ct)
    {
        var validation = await logoutValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return ValidationProblem(validation.ToString());

        await authService.LogoutAsync(request.RefreshToken, ct);
        return NoContent();
    }
}
