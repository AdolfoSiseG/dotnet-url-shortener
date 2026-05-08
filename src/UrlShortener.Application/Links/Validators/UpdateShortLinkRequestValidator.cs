using FluentValidation;
using UrlShortener.Application.Links.Dtos;

namespace UrlShortener.Application.Links.Validators;

public class UpdateShortLinkRequestValidator : AbstractValidator<UpdateShortLinkRequest>
{
    public UpdateShortLinkRequestValidator()
    {
        RuleFor(x => x.Title).MaximumLength(256);

        RuleFor(x => x.ExpiresAt!.Value)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("ExpiresAt must be in the future.")
            .When(x => x.ExpiresAt.HasValue);
    }
}
