using FluentValidation;

namespace Rawaj.Application.Features.Campaigns.GenerateCampaignContent;

public class GenerateCampaignContentCommandValidator : AbstractValidator<GenerateCampaignContentCommand>
{
    public GenerateCampaignContentCommandValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.PostCount).InclusiveBetween(1, 15);
    }
}
