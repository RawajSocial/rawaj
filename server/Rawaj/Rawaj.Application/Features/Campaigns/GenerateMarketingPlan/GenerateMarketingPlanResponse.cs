namespace Rawaj.Application.Features.Campaigns.GenerateMarketingPlan;

public record GenerateMarketingPlanResponse(Guid CampaignId, string AiPlanJson, DateTime AiGeneratedAt);
