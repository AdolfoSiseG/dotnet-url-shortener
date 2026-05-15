using FluentValidation;
using UrlShortener.Application.Auth.Dtos;

namespace UrlShortener.Application.Auth.Validators;

public class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(256);
    }
}
