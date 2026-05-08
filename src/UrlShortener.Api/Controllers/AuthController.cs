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
    IValidator<LoginRequest> loginValidator) : ControllerBase
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

        // Local try/catch is interim for week 3. Week 10 introduces a global
        // error middleware that translates DomainException to HTTP responses.
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
}
