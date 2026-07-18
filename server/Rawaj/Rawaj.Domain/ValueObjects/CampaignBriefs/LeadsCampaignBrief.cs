namespace Rawaj.Domain.ValueObjects.CampaignBriefs;

public class LeadsCampaignBrief : CampaignBrief
{
    public string? LeadAction { get; set; }
    public string? HasLandingPage { get; set; }
    public string? LandingPageUrl { get; set; }
    public string? BrandStatusForLeads { get; set; }
    public string? ContentFeeling { get; set; }
}
