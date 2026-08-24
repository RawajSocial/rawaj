using FluentValidation;
using Rawaj.Application.Common.Validation;

namespace Rawaj.Application.Features.Brands.CreateBrandProfile;

public class CreateBrandProfileCommandValidator : AbstractValidator<CreateBrandProfileCommand>
{
    public CreateBrandProfileCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleForEach(x => x.Tones).IsInEnum();
        RuleFor(x => x.Tones).Must(t => t == null || t.Count <= 5)
            .WithMessage("You can select at most 5 tone words.");
        RuleFor(x => x.WebsiteUrl).Must(UrlNormalizer.IsValidUrl).When(x => !string.IsNullOrWhiteSpace(x.WebsiteUrl))
            .WithMessage("Website URL must be a valid URL.");
        RuleFor(x => x.LogoUrl).Must(BeAValidLogoUrl).When(x => !string.IsNullOrWhiteSpace(x.LogoUrl))
            .WithMessage("Logo URL must be a valid absolute URL or a /media/ path.");
    }

    // Logos uploaded via POST /brand-profiles/logo are stored as root-relative /media/ paths,
    // so accept those in addition to absolute URLs (e.g. legacy data: URLs).
    private static bool BeAValidLogoUrl(string? url) =>
        url is not null && (url.StartsWith("/media/", StringComparison.Ordinal) || UrlNormalizer.IsValidUrl(url));
}
