namespace Rawaj.Application.Features.Campaigns.Common.CampaignBriefRequests;

public class NewProductCampaignBriefRequest : CampaignBriefRequest
{
    public string? ProductName { get; set; }
    public string? ProductCategory { get; set; }
    public string? ProductAvailability { get; set; }
    public string? ProductPricePoint { get; set; }
}
