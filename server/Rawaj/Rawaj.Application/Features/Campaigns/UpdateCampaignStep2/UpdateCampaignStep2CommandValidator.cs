using FluentValidation;
using Rawaj.Application.Features.Campaigns.Common.CampaignBriefRequests;

namespace Rawaj.Application.Features.Campaigns.UpdateCampaignStep2;

public class UpdateCampaignStep2CommandValidator : AbstractValidator<UpdateCampaignStep2Command>
{
    public UpdateCampaignStep2CommandValidator()
    {
        RuleFor(x => x.CampaignStartDate).NotEmpty();
        RuleFor(x => x.Brief).NotNull();

        RuleFor(x => x.Brief).SetInheritanceValidator(v =>
        {
            v.Add<NewBusinessCampaignBriefRequest>(new NewBusinessCampaignBriefRequestValidator());
            v.Add<NewProductCampaignBriefRequest>(new NewProductCampaignBriefRequestValidator());
            v.Add<DriveSalesCampaignBriefRequest>(new DriveSalesCampaignBriefRequestValidator());
            v.Add<SeasonalCampaignBriefRequest>(new SeasonalCampaignBriefRequestValidator());
            v.Add<LeadsCampaignBriefRequest>(new LeadsCampaignBriefRequestValidator());
            v.Add<AwarenessCampaignBriefRequest>(new AwarenessCampaignBriefRequestValidator());
            v.Add<OtherCampaignBriefRequest>(new OtherCampaignBriefRequestValidator());
        });
    }
}
