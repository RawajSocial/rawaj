using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Campaigns.Common;

namespace Rawaj.Application.Features.Campaigns.UpdateCampaignStep6;

public class UpdateCampaignStep6CommandHandler(
    ICampaignOnboardingService onboardingService,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateCampaignStep6Command, Result<CampaignResponse>>
{
    public async Task<Result<CampaignResponse>> Handle(UpdateCampaignStep6Command request, CancellationToken cancellationToken)
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

        if (!CampaignOnboardingStepRules.CanApplyStep(campaign.CurrentStep, 6))
        {
            return Result<CampaignResponse>.Failure("Cannot skip ahead. Complete previous steps first.");
        }

        campaign.CampaignPhotoUrls = request.CampaignPhotoUrls;
        campaign.Hashtags = request.Hashtags;
        campaign.AdditionalNotes = request.AdditionalNotes;
        campaign.FacebookConnected = request.Facebook?.Connected;
        campaign.FacebookAccountName = request.Facebook?.AccountName;
        campaign.InstagramConnected = request.Instagram?.Connected;
        campaign.InstagramAccountName = request.Instagram?.AccountName;

        CampaignOnboardingStepRules.AdvanceStep(campaign, 6);
        campaign.UpdatedAt = DateTime.UtcNow;

        await onboardingService.SaveChangesAsync(cancellationToken);

        return Result<CampaignResponse>.Success(CampaignResponse.FromEntity(campaign));
    }
}
