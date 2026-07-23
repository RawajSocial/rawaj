using FluentValidation;
using Rawaj.Application.Common.Validation;

namespace Rawaj.Application.Features.Tenants.UpgradeToAgency;

public class UpgradeToAgencyCommandValidator : AbstractValidator<UpgradeToAgencyCommand>
{
    public UpgradeToAgencyCommandValidator()
    {
        RuleFor(x => x.AgencySize).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ServicesOffered).NotEmpty().WithMessage("Select at least one service you offer.");
        RuleFor(x => x.Website).Must(UrlNormalizer.IsValidUrl).When(x => !string.IsNullOrWhiteSpace(x.Website))
            .WithMessage("Website must be a valid URL.");
    }
}
