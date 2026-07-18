using FluentValidation;

namespace Rawaj.Application.Features.Campaigns.Common.CampaignBriefRequests;

public class NewBusinessCampaignBriefRequestValidator : AbstractValidator<NewBusinessCampaignBriefRequest>
{
    public NewBusinessCampaignBriefRequestValidator()
    {
        RuleFor(x => x.BusinessLaunchDate).NotEmpty();
    }
}

public class NewProductCampaignBriefRequestValidator : AbstractValidator<NewProductCampaignBriefRequest>
{
    public NewProductCampaignBriefRequestValidator()
    {
        RuleFor(x => x.ProductName).NotEmpty();
    }
}

public class DriveSalesCampaignBriefRequestValidator : AbstractValidator<DriveSalesCampaignBriefRequest>
{
    public DriveSalesCampaignBriefRequestValidator()
    {
        RuleFor(x => x.SalesScope).NotEmpty();
    }
}

public class SeasonalCampaignBriefRequestValidator : AbstractValidator<SeasonalCampaignBriefRequest>
{
    public SeasonalCampaignBriefRequestValidator()
    {
        RuleFor(x => x.Occasion).NotEmpty();
        RuleFor(x => x.SeasonStart).NotEmpty();
        RuleFor(x => x.SeasonEnd).NotEmpty();
    }
}

public class LeadsCampaignBriefRequestValidator : AbstractValidator<LeadsCampaignBriefRequest>
{
    public LeadsCampaignBriefRequestValidator()
    {
        RuleFor(x => x.LeadAction).NotEmpty();
    }
}

public class AwarenessCampaignBriefRequestValidator : AbstractValidator<AwarenessCampaignBriefRequest>
{
    public AwarenessCampaignBriefRequestValidator()
    {
        RuleFor(x => x.MainMessage).NotEmpty();
    }
}

public class OtherCampaignBriefRequestValidator : AbstractValidator<OtherCampaignBriefRequest>
{
    public OtherCampaignBriefRequestValidator()
    {
        RuleFor(x => x.CampaignDescription).NotEmpty();
    }
}
