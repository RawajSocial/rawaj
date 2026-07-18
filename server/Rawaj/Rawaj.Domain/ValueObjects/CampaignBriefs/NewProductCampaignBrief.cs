namespace Rawaj.Domain.ValueObjects.CampaignBriefs;

public class NewProductCampaignBrief : CampaignBrief
{
    public string? ProductName { get; set; }
    public string? ProductCategory { get; set; }
    public string? ProductAvailability { get; set; }
    public string? ProductPricePoint { get; set; }
}
