using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Application.Auth.Dtos;
using UrlShortener.Application.Auth.Interfaces;
using UrlShortener.Domain.Exceptions;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IAuthService authService,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IValidator<RefreshRequest> refreshValidator,
    IValidator<LogoutRequest> logoutValidator) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken ct)
    {
        var validation = await registerValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToString());
        }

        // Local try/catch is interim. Week 10 introduces a global error
        // middleware that translates DomainException to HTTP responses.
        try
        {
            return Ok(await authService.RegisterAsync(request, ct));
        }
        catch (EmailAlreadyExistsException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var validation = await loginValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToString());
        }

        try
        {
            return Ok(await authService.LoginAsync(request, ct));
        }
        catch (InvalidCredentialsException)
        {
            return Unauthorized(new { error = "Email or password is incorrect." });
        }
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken ct)
    {
        var validation = await refreshValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToString());
        }

        try
        {
            return Ok(await authService.RefreshAsync(request.RefreshToken, ct));
        }
        catch (InvalidRefreshTokenException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    // Idempotent: returns 204 whether or not the token existed or was
    // already revoked. The client can safely retry on transient failures.
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken ct)
    {
        var validation = await logoutValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToString());
        }

        await authService.LogoutAsync(request.RefreshToken, ct);
        return NoContent();
    }
}
