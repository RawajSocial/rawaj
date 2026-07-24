using FluentValidation;

namespace Rawaj.Application.Features.Campaigns.RefineCampaignPlan;

public class RefineCampaignPlanCommandValidator : AbstractValidator<RefineCampaignPlanCommand>
{
    public RefineCampaignPlanCommandValidator()
    {
        RuleFor(x => x.Feedback).NotEmpty().MaximumLength(2000);
    }
}
