using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Campaigns.Common;

namespace Rawaj.Application.Features.Campaigns.UpdateCampaignStep4;

public class UpdateCampaignStep4CommandHandler(
    ICampaignOnboardingService onboardingService,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateCampaignStep4Command, Result<CampaignResponse>>
{
    public async Task<Result<CampaignResponse>> Handle(UpdateCampaignStep4Command request, CancellationToken cancellationToken)
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

        if (!CampaignOnboardingStepRules.CanApplyStep(campaign.CurrentStep, 4))
        {
            return Result<CampaignResponse>.Failure("Cannot skip ahead. Complete previous steps first.");
        }

        campaign.AudienceGender = request.Gender;
        campaign.CustomerType = request.CustomerType;
        campaign.AgeRanges = request.AgeRanges;
        campaign.IncomeLevel = request.IncomeLevel;
        campaign.CustomerLocation = request.CustomerLocation;
        campaign.EducationLevel = request.EducationLevel;
        campaign.TargetDescription = request.TargetDescription;
        campaign.Interests = request.Interests;
        campaign.PainPoints = request.PainPoints;
        campaign.BuyingBehavior = request.BuyingBehavior;
        campaign.HasExistingCustomers = request.HasExistingCustomers;
        campaign.AudiencePlatforms = request.AudiencePlatforms;

        CampaignOnboardingStepRules.AdvanceStep(campaign, 4);
        campaign.UpdatedAt = DateTime.UtcNow;

        await onboardingService.SaveChangesAsync(cancellationToken);

        return Result<CampaignResponse>.Success(CampaignResponse.FromEntity(campaign));
    }
}
