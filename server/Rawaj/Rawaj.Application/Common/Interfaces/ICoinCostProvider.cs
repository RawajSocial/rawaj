namespace Rawaj.Application.Common.Interfaces;

/// <summary>
/// Coin cost per action, before any per-plan discount (see <c>CoinPricingPolicy</c> for the
/// discounted price actually charged, and <c>CoinPolicy</c> for how a cost is debited).
/// Configurable so pricing can change without a redeploy touching Application code.
/// </summary>
public interface ICoinCostProvider
{
    /// <summary>Single Social Media Post Generation.</summary>
    int ContentGeneration { get; }
    /// <summary>Single AI Image Generation.</summary>
    int VisualGeneration { get; }
    /// <summary>Bundled multi-post campaign content batch — an approximation, since the AI model
    /// decides how many posts to produce per batch rather than the caller.</summary>
    int CampaignContentGeneration { get; }
    /// <summary>Complete Marketing Strategy.</summary>
    int MarketingPlanGeneration { get; }
    /// <summary>Schedule one post/story/reel/carousel/promotional design.</summary>
    int Scheduling { get; }

    /// <summary>Reserved pricing only — no feature spends this yet (see PROGRESS.md). Exposed here
    /// purely so the public pricing page and <c>GetCoinPricingQuery</c> can display the full price
    /// list without drifting from what will eventually be charged.</summary>
    int BusinessDiagnosis { get; }
    int CompetitiveAnalysis { get; }
    int ReasoningConversation { get; }
}
