using FluentValidation;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.UpdateCampaign;

/// <remarks>
/// Every message here is written out explicitly instead of relying on FluentValidation's defaults:
/// the client translates these exact strings into Arabic (see api-error.util.ts's
/// KNOWN_MESSAGE_TRANSLATIONS), and the defaults interpolate the property name, so they could
/// never match a lookup and surfaced to the user as a bare "Validation failed." instead.
/// </remarks>
public class UpdateCampaignCommandValidator : AbstractValidator<UpdateCampaignCommand>
{
    public UpdateCampaignCommandValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Campaign name is required.")
            .MaximumLength(255).WithMessage("Campaign name must be 255 characters or fewer.")
            .When(x => x.Name is not null);

        RuleFor(x => x.Objective)
            .MaximumLength(255).WithMessage("Objective must be 255 characters or fewer.")
            .When(x => x.Objective is not null);

        RuleFor(x => x.BudgetAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Budget cannot be negative.")
            .When(x => x.BudgetAmount.HasValue);

        // Only checks a patch that supplies both ends. One that moves just one of the two is
        // validated in the handler against the value already stored, which this rule can't see.
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("End date must be on or after the start date.");

        When(x => x.TargetPlatforms is not null, () =>
        {
            RuleFor(x => x.TargetPlatforms)
                .NotEmpty().WithMessage("At least one target platform is required.");

            // Mirrors CreateCampaignCommandValidator. Without it, an unparseable platform reached
            // the handler's `Enum.Parse` and came back as an unhandled 500.
            RuleForEach(x => x.TargetPlatforms)
                .Must(p => Enum.TryParse<SocialPlatform>(p, true, out _))
                .WithMessage("'{PropertyValue}' is not a supported platform.");
        });
    }
}
