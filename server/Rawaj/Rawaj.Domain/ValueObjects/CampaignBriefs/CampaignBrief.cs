using System.Text.Json.Serialization;
using Rawaj.Domain.Entities.Campaigns;

namespace Rawaj.Domain.ValueObjects.CampaignBriefs;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "campaignType")]
[JsonDerivedType(typeof(NewBusinessCampaignBrief), MarketingCampaign.CampaignTypes.NewBusiness)]
[JsonDerivedType(typeof(NewProductCampaignBrief), MarketingCampaign.CampaignTypes.NewProduct)]
[JsonDerivedType(typeof(DriveSalesCampaignBrief), MarketingCampaign.CampaignTypes.DriveSales)]
[JsonDerivedType(typeof(SeasonalCampaignBrief), MarketingCampaign.CampaignTypes.Seasonal)]
[JsonDerivedType(typeof(LeadsCampaignBrief), MarketingCampaign.CampaignTypes.Leads)]
[JsonDerivedType(typeof(AwarenessCampaignBrief), MarketingCampaign.CampaignTypes.Awareness)]
[JsonDerivedType(typeof(OtherCampaignBrief), MarketingCampaign.CampaignTypes.Other)]
public abstract class CampaignBrief
{
}
