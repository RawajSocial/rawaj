using Rawaj.Application.Common.Interfaces;

namespace Rawaj.Infrastructure;

public class CoinCostSettings : ICoinCostProvider
{
    public const string SectionName = "Coins";

    public int ContentGeneration { get; set; } = 5;
    public int VisualGeneration { get; set; } = 10;
    public int CampaignContentGeneration { get; set; } = 20;
    public int MarketingPlanGeneration { get; set; } = 30;
}
