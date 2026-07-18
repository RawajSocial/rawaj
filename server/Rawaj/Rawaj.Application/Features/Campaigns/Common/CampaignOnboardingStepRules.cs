using Rawaj.Domain.Entities.Campaigns;

namespace Rawaj.Application.Features.Campaigns.Common;

public static class CampaignOnboardingStepRules
{
    public static bool CanApplyStep(int currentStep, int targetStep) => targetStep <= currentStep + 1;

    public static void AdvanceStep(MarketingCampaign campaign, int targetStep) =>
        campaign.CurrentStep = Math.Max(campaign.CurrentStep, targetStep);
}
