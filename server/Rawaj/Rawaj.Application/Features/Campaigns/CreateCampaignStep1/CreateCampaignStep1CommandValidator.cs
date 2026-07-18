using FluentValidation;
using Rawaj.Domain.Entities.Campaigns;

namespace Rawaj.Application.Features.Campaigns.CreateCampaignStep1;

public class CreateCampaignStep1CommandValidator : AbstractValidator<CreateCampaignStep1Command>
{
    public CreateCampaignStep1CommandValidator()
    {
        RuleFor(x => x.BrandProfileId).NotEmpty();
        RuleFor(x => x.CampaignType)
            .NotEmpty()
            .Must(t => MarketingCampaign.CampaignTypes.All.Contains(t))
            .WithMessage("Invalid campaign type.");
    }
}
