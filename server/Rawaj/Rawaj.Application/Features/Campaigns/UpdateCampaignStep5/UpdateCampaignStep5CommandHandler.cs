using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Campaigns.Common;

namespace Rawaj.Application.Features.Campaigns.UpdateCampaignStep5;

public class UpdateCampaignStep5CommandHandler(
    ICampaignOnboardingService onboardingService,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateCampaignStep5Command, Result<CampaignResponse>>
{
    public async Task<Result<CampaignResponse>> Handle(UpdateCampaignStep5Command request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } userId)
        {
            return Result<CampaignResponse>.Failure("Not authenticated.");
        }

        var campaign = await onboardingService.GetByIdAsync(request.CampaignId, cancellationToken);
        if (campaign is null)
        {
            return Result<CampaignResponse>.Failure("Campaign not found.");
        }

        var hasAccess = await onboardingService.UserHasBrandAccessAsync(userId, campaign.BrandProfileId, cancellationToken);
        if (!hasAccess)
        {
            return Result<CampaignResponse>.Failure("You do not have access to this campaign.");
        }

        if (!CampaignOnboardingStepRules.CanApplyStep(campaign.CurrentStep, 5))
        {
            return Result<CampaignResponse>.Failure("Cannot skip ahead. Complete previous steps first.");
        }

        campaign.PositioningVs = request.PositioningVs;
        campaign.CampaignOutcome = request.CampaignOutcome;
        campaign.SuccessMetrics = request.SuccessMetrics;
        campaign.BrandsAdmired = request.BrandsAdmired;
        campaign.MonthlyBudget = request.MonthlyBudget;
        campaign.BudgetFrom = request.BudgetFrom;
        campaign.BudgetTo = request.BudgetTo;
        campaign.PlatformRanking = request.PlatformRanking;
        campaign.Goals = request.Goals;
        campaign.Timeframe = request.Timeframe;
        campaign.AgencyExperience = request.AgencyExperience;
        campaign.TargetSales = request.TargetSales;

        CampaignOnboardingStepRules.AdvanceStep(campaign, 5);
        campaign.UpdatedAt = DateTime.UtcNow;

        await onboardingService.SaveChangesAsync(cancellationToken);

        return Result<CampaignResponse>.Success(CampaignResponse.FromEntity(campaign));
    }
}
