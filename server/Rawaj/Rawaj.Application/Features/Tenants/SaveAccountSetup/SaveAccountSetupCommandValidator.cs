using FluentValidation;

namespace Rawaj.Application.Features.Tenants.SaveAccountSetup;

public class SaveAccountSetupCommandValidator : AbstractValidator<SaveAccountSetupCommand>
{
    public SaveAccountSetupCommandValidator()
    {
        RuleFor(x => x.BrandProfileId).NotEmpty();
        RuleFor(x => x.AccountType).NotEmpty().Must(v => v is "agency" or "business")
            .WithMessage("AccountType must be either 'agency' or 'business'.");
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.Website).Must(BeAValidUrl).WithMessage("Website must be a valid absolute URL.");

        When(x => x.AccountType == "agency", () =>
        {
            RuleFor(x => x.AgencyName).NotEmpty().WithMessage("AgencyName is required for agency accounts.");
        });

        When(x => x.AccountType == "business", () =>
        {
            RuleFor(x => x.BusinessName).NotEmpty().WithMessage("BusinessName is required for business accounts.");
        });
    }

    private static bool BeAValidUrl(string? url) =>
        url is null || Uri.TryCreate(url, UriKind.Absolute, out _);
}
