using FluentValidation;

namespace Rawaj.Application.Features.Competitors.ScrapeWebsite;

public class ScrapeWebsiteCommandValidator : AbstractValidator<ScrapeWebsiteCommand>
{
    public ScrapeWebsiteCommandValidator()
    {
        RuleFor(x => x.CompetitorId).NotEmpty();
        RuleFor(x => x.Url)
            .NotEmpty()
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Url must be an absolute http or https URL.");
    }
}
