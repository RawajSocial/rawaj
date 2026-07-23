using FluentValidation;

namespace Rawaj.Application.Features.Campaigns.UpdateCampaign;

public class UpdateCampaignCommandValidator : AbstractValidator<UpdateCampaignCommand>
{
    public UpdateCampaignCommandValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();

        RuleFor(x => x.Name).MaximumLength(255).When(x => x.Name is not null);
        RuleFor(x => x.Name).NotEmpty().When(x => x.Name is not null).WithMessage("'Name' must not be empty.");

        RuleFor(x => x.BudgetAmount).GreaterThanOrEqualTo(0).When(x => x.BudgetAmount.HasValue);

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("End date must be on or after the start date.");
    }
}
