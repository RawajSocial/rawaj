using FluentValidation;

namespace Rawaj.Application.Features.Campaigns.UpdateCampaignStep7;

public class UpdateCampaignStep7CommandValidator : AbstractValidator<UpdateCampaignStep7Command>
{
    public UpdateCampaignStep7CommandValidator()
    {
        RuleFor(x => x.Answers).NotEmpty();
    }
}
