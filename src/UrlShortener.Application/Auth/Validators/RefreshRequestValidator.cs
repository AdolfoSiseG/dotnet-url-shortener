using FluentValidation;
using UrlShortener.Application.Auth.Dtos;

namespace UrlShortener.Application.Auth.Validators;

public class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        // Tokens are 64-byte base64 (~88 chars). 256 leaves slack for any
        // future encoding change without revisiting the validator.
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(256);
    }
}
