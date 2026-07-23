namespace Rawaj.Application.Common.Interfaces;

/// <summary>
/// Coin cost per AI action — configurable so pricing can change without a redeploy touching
/// Application code. See <c>CoinPolicy</c> for how a cost is actually debited.
/// </summary>
public interface ICoinCostProvider
{
    int ContentGeneration { get; }
    int VisualGeneration { get; }
    int CampaignContentGeneration { get; }
    int MarketingPlanGeneration { get; }
}
