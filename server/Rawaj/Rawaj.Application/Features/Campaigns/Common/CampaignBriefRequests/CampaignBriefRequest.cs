using System.Text.Json.Serialization;
using Rawaj.Domain.Entities.Campaigns;

namespace Rawaj.Application.Features.Campaigns.Common.CampaignBriefRequests;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "campaignType")]
[JsonDerivedType(typeof(NewBusinessCampaignBriefRequest), MarketingCampaign.CampaignTypes.NewBusiness)]
[JsonDerivedType(typeof(NewProductCampaignBriefRequest), MarketingCampaign.CampaignTypes.NewProduct)]
[JsonDerivedType(typeof(DriveSalesCampaignBriefRequest), MarketingCampaign.CampaignTypes.DriveSales)]
[JsonDerivedType(typeof(SeasonalCampaignBriefRequest), MarketingCampaign.CampaignTypes.Seasonal)]
[JsonDerivedType(typeof(LeadsCampaignBriefRequest), MarketingCampaign.CampaignTypes.Leads)]
[JsonDerivedType(typeof(AwarenessCampaignBriefRequest), MarketingCampaign.CampaignTypes.Awareness)]
[JsonDerivedType(typeof(OtherCampaignBriefRequest), MarketingCampaign.CampaignTypes.Other)]
public abstract class CampaignBriefRequest
{
}
