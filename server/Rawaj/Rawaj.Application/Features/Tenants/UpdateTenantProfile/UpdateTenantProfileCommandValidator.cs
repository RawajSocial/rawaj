using FluentValidation;
using Rawaj.Application.Common.Validation;

namespace Rawaj.Application.Features.Tenants.UpdateTenantProfile;

public class UpdateTenantProfileCommandValidator : AbstractValidator<UpdateTenantProfileCommand>
{
    public UpdateTenantProfileCommandValidator()
    {
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Industry).MaximumLength(100);
        RuleFor(x => x.Country).MaximumLength(100);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.Website).Must(UrlNormalizer.IsValidUrl).When(x => !string.IsNullOrWhiteSpace(x.Website))
            .WithMessage("Website must be a valid URL.");
    }
}
