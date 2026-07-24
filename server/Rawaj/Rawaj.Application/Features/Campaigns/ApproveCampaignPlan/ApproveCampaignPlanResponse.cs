using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.ApproveCampaignPlan;

public record ApproveCampaignPlanResponse(Guid CampaignId, CampaignStatus Status, DateTime PlanApprovedAt);
