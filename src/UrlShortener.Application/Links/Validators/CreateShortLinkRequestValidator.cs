using FluentValidation;
using UrlShortener.Application.Links.Dtos;

namespace UrlShortener.Application.Links.Validators;

public class CreateShortLinkRequestValidator : AbstractValidator<CreateShortLinkRequest>
{
    private static readonly char[] Base62Chars =
        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();

    public CreateShortLinkRequestValidator()
    {
        RuleFor(x => x.OriginalUrl)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(BeAbsoluteHttpUrl)
            .WithMessage("OriginalUrl must be an absolute http or https URL.");

        RuleFor(x => x.Title).MaximumLength(256);

        RuleFor(x => x.CustomSlug!)
            .Length(4, 20)
            .Must(BeBase62)
            .WithMessage("CustomSlug must contain only base62 characters (letters and digits).")
            .When(x => !string.IsNullOrEmpty(x.CustomSlug));

        RuleFor(x => x.ExpiresAt!.Value)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("ExpiresAt must be in the future.")
            .When(x => x.ExpiresAt.HasValue);

        RuleFor(x => x.Password!)
            .MinimumLength(6)
            .When(x => !string.IsNullOrEmpty(x.Password));
    }

    private static bool BeAbsoluteHttpUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static bool BeBase62(string slug)
    {
        foreach (var c in slug)
        {
            if (Array.IndexOf(Base62Chars, c) < 0) return false;
        }
        return true;
    }
}
