using FluentValidation;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.CreateCampaign;

public class CreateCampaignCommandValidator : AbstractValidator<CreateCampaignCommand>
{
    public CreateCampaignCommandValidator()
    {
        RuleFor(x => x.BrandProfileId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);

        RuleFor(x => x.TargetPlatforms).NotEmpty().WithMessage("At least one target platform is required.");

        RuleForEach(x => x.TargetPlatforms)
            .Must(p => Enum.TryParse<SocialPlatform>(p, true, out _))
            .WithMessage("'{PropertyValue}' is not a valid platform.");

        RuleFor(x => x.BudgetAmount).GreaterThanOrEqualTo(0).When(x => x.BudgetAmount.HasValue);

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("End date must be on or after the start date.");
    }
}
