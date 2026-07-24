using Rawaj.Application.Common.Interfaces;

namespace Rawaj.Infrastructure;

public class CoinCostSettings : ICoinCostProvider
{
    public const string SectionName = "Coins";

    public int ContentGeneration { get; set; } = 200;
    public int VisualGeneration { get; set; } = 400;
    public int CampaignContentGeneration { get; set; } = 1000;
    public int MarketingPlanGeneration { get; set; } = 12000;
    public int Scheduling { get; set; } = 100;

    public int BusinessDiagnosis { get; set; } = 4000;
    public int CompetitiveAnalysis { get; set; } = 3500;
    public int ReasoningConversation { get; set; } = 2500;
}
