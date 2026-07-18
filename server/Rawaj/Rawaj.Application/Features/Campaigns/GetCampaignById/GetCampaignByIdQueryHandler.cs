using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Campaigns.Common;

namespace Rawaj.Application.Features.Campaigns.GetCampaignById;

public class GetCampaignByIdQueryHandler(
    ICampaignOnboardingService onboardingService,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetCampaignByIdQuery, Result<CampaignResponse>>
{
    public async Task<Result<CampaignResponse>> Handle(GetCampaignByIdQuery request, CancellationToken cancellationToken)
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

        return Result<CampaignResponse>.Success(CampaignResponse.FromEntity(campaign));
    }
}
