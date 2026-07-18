namespace Rawaj.Application.Features.Campaigns.Common.CampaignBriefRequests;

public class DriveSalesCampaignBriefRequest : CampaignBriefRequest
{
    public string? SalesScope { get; set; }
    public string? HasOffer { get; set; }
    public string? OfferDetails { get; set; }
    public string? SalesPeriod { get; set; }
}
