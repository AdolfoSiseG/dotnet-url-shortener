using FluentValidation;
using UrlShortener.Application.Analytics.Dtos;

namespace UrlShortener.Application.Analytics.Validators;

public class ByTimeQueryValidator : AbstractValidator<ByTimeQuery>
{
    private static readonly HashSet<string> AllowedGranularities =
        new(StringComparer.OrdinalIgnoreCase) { "day", "week", "month" };

    private const int MaxRangeDays = 366;

    public ByTimeQueryValidator()
    {
        RuleFor(q => q.From)
            .LessThan(q => q.To)
            .WithMessage("'from' must be earlier than 'to'.");

        RuleFor(q => q.Granularity)
            .Must(g => AllowedGranularities.Contains(g))
            .WithMessage("Granularity must be one of: day, week, month.");

        // Caps the range so a client cannot ask for a 100-year time series
        // and starve the worker. One year + leap-day slack is enough for
        // the chart granularities we support.
        RuleFor(q => q)
            .Must(q => (q.To - q.From).TotalDays <= MaxRangeDays)
            .WithMessage($"Range must be at most {MaxRangeDays} days.");
    }
}
