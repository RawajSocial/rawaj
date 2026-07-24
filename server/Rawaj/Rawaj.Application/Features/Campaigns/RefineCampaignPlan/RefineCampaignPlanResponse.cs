namespace Rawaj.Application.Features.Campaigns.RefineCampaignPlan;

public record RefineCampaignPlanResponse(Guid CampaignId, string AiPlanJson, DateTime AiGeneratedAt);
