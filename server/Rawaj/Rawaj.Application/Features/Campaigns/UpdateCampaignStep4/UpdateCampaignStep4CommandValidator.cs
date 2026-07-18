using FluentValidation;

namespace Rawaj.Application.Features.Campaigns.UpdateCampaignStep4;

public class UpdateCampaignStep4CommandValidator : AbstractValidator<UpdateCampaignStep4Command>
{
    public UpdateCampaignStep4CommandValidator()
    {
        RuleFor(x => x.Gender)
            .Must(g => g is null or "female" or "male" or "all")
            .WithMessage("Gender must be one of: female, male, all.");
    }
}
