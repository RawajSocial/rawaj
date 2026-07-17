using FluentValidation;

namespace Rawaj.Application.Features.Brands.UpdateBrandProfile;

public class UpdateBrandProfileCommandValidator : AbstractValidator<UpdateBrandProfileCommand>
{
    public UpdateBrandProfileCommandValidator()
    {
        RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.BrandVoice).IsInEnum().When(x => x.BrandVoice.HasValue);
        RuleFor(x => x.WebsiteUrl).Must(BeAValidUrl).When(x => !string.IsNullOrWhiteSpace(x.WebsiteUrl))
            .WithMessage("Website URL must be a valid absolute URL.");
        RuleFor(x => x.LogoUrl).Must(BeAValidUrl).When(x => !string.IsNullOrWhiteSpace(x.LogoUrl))
            .WithMessage("Logo URL must be a valid absolute URL.");
    }

    private static bool BeAValidUrl(string? url) => Uri.TryCreate(url, UriKind.Absolute, out _);
}
