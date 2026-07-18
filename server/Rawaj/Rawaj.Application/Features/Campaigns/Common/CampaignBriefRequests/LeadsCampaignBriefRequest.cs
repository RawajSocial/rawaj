namespace Rawaj.Application.Features.Campaigns.Common.CampaignBriefRequests;

public class LeadsCampaignBriefRequest : CampaignBriefRequest
{
    public string? LeadAction { get; set; }
    public string? HasLandingPage { get; set; }
    public string? LandingPageUrl { get; set; }
    public string? BrandStatusForLeads { get; set; }
    public string? ContentFeeling { get; set; }
}
