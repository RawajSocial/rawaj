using FluentValidation;

namespace Rawaj.Application.Features.Tenants.UpdateBrandProfile;

public class UpdateBrandProfileCommandValidator : AbstractValidator<UpdateBrandProfileCommand>
{
    public UpdateBrandProfileCommandValidator()
    {
        RuleFor(x => x.BrandProfileId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BrandVoice!.Value).IsInEnum().When(x => x.BrandVoice.HasValue);

        RuleFor(x => x.WebsiteUrl).Must(BeAValidUrl).WithMessage("WebsiteUrl must be a valid absolute URL.");
        RuleFor(x => x.LogoUrl).Must(BeAValidUrl).WithMessage("LogoUrl must be a valid absolute URL.");

        RuleFor(x => x.Colors).Must(c => c == null || c.Count <= 10).WithMessage("Colors can contain at most 10 items.");
        RuleFor(x => x.SupportedLanguages).Must(l => l == null || l.Count <= 10).WithMessage("SupportedLanguages can contain at most 10 items.");
        RuleFor(x => x.Keywords).Must(k => k == null || k.Count <= 20).WithMessage("Keywords can contain at most 20 items.");
    }

    private static bool BeAValidUrl(string? url) =>
        url is null || Uri.TryCreate(url, UriKind.Absolute, out _);
}
