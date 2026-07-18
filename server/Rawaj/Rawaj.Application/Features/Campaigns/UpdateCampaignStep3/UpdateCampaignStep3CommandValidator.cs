using FluentValidation;

namespace Rawaj.Application.Features.Campaigns.UpdateCampaignStep3;

public class UpdateCampaignStep3CommandValidator : AbstractValidator<UpdateCampaignStep3Command>
{
    public UpdateCampaignStep3CommandValidator()
    {
        RuleFor(x => x.BrandWords)
            .Must(w => w == null || w.Count <= 3)
            .WithMessage("BrandWords can contain at most 3 items.");
    }
}
