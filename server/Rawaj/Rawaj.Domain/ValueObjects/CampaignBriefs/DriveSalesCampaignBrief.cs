namespace Rawaj.Domain.ValueObjects.CampaignBriefs;

public class DriveSalesCampaignBrief : CampaignBrief
{
    public string? SalesScope { get; set; }
    public string? HasOffer { get; set; }
    public string? OfferDetails { get; set; }
    public string? SalesPeriod { get; set; }
}
