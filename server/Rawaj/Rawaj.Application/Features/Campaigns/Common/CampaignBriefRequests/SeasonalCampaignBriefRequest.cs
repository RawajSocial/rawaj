namespace Rawaj.Application.Features.Campaigns.Common.CampaignBriefRequests;

public class SeasonalCampaignBriefRequest : CampaignBriefRequest
{
    public string? Occasion { get; set; }
    public string? SeasonStart { get; set; }
    public string? SeasonEnd { get; set; }
}
