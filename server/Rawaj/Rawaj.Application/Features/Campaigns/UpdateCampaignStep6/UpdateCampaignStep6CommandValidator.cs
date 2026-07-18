using FluentValidation;

namespace Rawaj.Application.Features.Campaigns.UpdateCampaignStep6;

public class UpdateCampaignStep6CommandValidator : AbstractValidator<UpdateCampaignStep6Command>
{
    public UpdateCampaignStep6CommandValidator()
    {
        RuleFor(x => x.CampaignPhotoUrls)
            .Must(p => p == null || p.Count <= 10)
            .WithMessage("CampaignPhotoUrls can contain at most 10 items.");
    }
}
