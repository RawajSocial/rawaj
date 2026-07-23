using FluentValidation;
using Rawaj.Application.Common.Validation;

namespace Rawaj.Application.Features.Brands.UpdateBrandProfile;

public class UpdateBrandProfileCommandValidator : AbstractValidator<UpdateBrandProfileCommand>
{
    public UpdateBrandProfileCommandValidator()
    {
        RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.BrandVoice).IsInEnum().When(x => x.BrandVoice.HasValue);
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
