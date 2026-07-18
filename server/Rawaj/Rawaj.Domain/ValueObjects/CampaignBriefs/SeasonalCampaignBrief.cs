namespace Rawaj.Domain.ValueObjects.CampaignBriefs;

public class SeasonalCampaignBrief : CampaignBrief
{
    public string? Occasion { get; set; }
    public string? SeasonStart { get; set; }
    public string? SeasonEnd { get; set; }
}
