using FluentValidation;

namespace Rawaj.Application.Features.Campaigns.UpdateCampaignStep5;

public class UpdateCampaignStep5CommandValidator : AbstractValidator<UpdateCampaignStep5Command>
{
    public UpdateCampaignStep5CommandValidator()
    {
        RuleFor(x => x.BrandsAdmired)
            .Must(b => b == null || b.Count <= 3)
            .WithMessage("BrandsAdmired can contain at most 3 items.");

        RuleFor(x => x.BudgetTo)
            .GreaterThanOrEqualTo(x => x.BudgetFrom!.Value)
            .When(x => x.BudgetFrom.HasValue && x.BudgetTo.HasValue)
            .WithMessage("BudgetTo must be greater than or equal to BudgetFrom.");
    }
}
