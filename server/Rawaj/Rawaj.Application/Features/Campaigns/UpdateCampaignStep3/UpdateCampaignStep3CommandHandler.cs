using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Campaigns.Common;

namespace Rawaj.Application.Features.Campaigns.UpdateCampaignStep3;

public class UpdateCampaignStep3CommandHandler(
    ICampaignOnboardingService onboardingService,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateCampaignStep3Command, Result<CampaignResponse>>
{
    public async Task<Result<CampaignResponse>> Handle(UpdateCampaignStep3Command request, CancellationToken cancellationToken)
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

        if (!CampaignOnboardingStepRules.CanApplyStep(campaign.CurrentStep, 3))
        {
            return Result<CampaignResponse>.Failure("Cannot skip ahead. Complete previous steps first.");
        }

        campaign.BrandName = request.BrandName;
        campaign.Tagline = request.Tagline;
        campaign.InstagramHandle = request.Instagram;
        campaign.Website = request.Website;
        campaign.Sector = request.Sector;
        campaign.Location = request.Location;
        campaign.BusinessAge = request.BusinessAge;
        campaign.Stage = request.Stage;
        campaign.BrandWords = request.BrandWords;
        campaign.BrandTone = request.BrandTone;
        campaign.HasBrandGuidelines = request.HasGuidelines;
        campaign.GuidelinesFileUrl = request.GuidelinesFileUrl;
        campaign.BrandColors = request.BrandColors;
        campaign.LogoFileUrl = request.LogoFileUrl;
        campaign.ContentLanguages = request.Languages;
        campaign.ProductDescription = request.ProductDesc;
        campaign.UniqueValueProposition = request.UniqueValue;
        campaign.PricePositioning = request.PricePositioning;
        campaign.StorePresence = request.StorePresence;
        campaign.ExistingPlatforms = request.ExistingPlatforms;

        CampaignOnboardingStepRules.AdvanceStep(campaign, 3);
        campaign.UpdatedAt = DateTime.UtcNow;

        await onboardingService.SaveChangesAsync(cancellationToken);

        return Result<CampaignResponse>.Success(CampaignResponse.FromEntity(campaign));
    }
}
