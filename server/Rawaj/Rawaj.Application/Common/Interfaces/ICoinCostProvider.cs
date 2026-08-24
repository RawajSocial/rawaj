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

    /// <summary>AI-generated brand/business diagnosis — charged by GenerateBusinessDiagnosisCommandHandler.</summary>
    int BusinessDiagnosis { get; }
    /// <summary>Competitor research pass — charged by ResearchCampaignCompetitorsCommandHandler.</summary>
    int CompetitiveAnalysis { get; }
    /// <summary>AI reasoning/conversation turns — charged by GenerateOnboardingQuestionsCommandHandler
    /// and RefineCampaignPlanCommandHandler.</summary>
    int ReasoningConversation { get; }
}
