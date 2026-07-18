using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Campaigns.Common;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.CreateCampaignStep1;

public class CreateCampaignStep1CommandHandler(
    ICampaignOnboardingService onboardingService,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateCampaignStep1Command, Result<CampaignResponse>>
{
    public async Task<Result<CampaignResponse>> Handle(CreateCampaignStep1Command request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } userId)
        {
            return Result<CampaignResponse>.Failure("Not authenticated.");
        }

        var hasAccess = await onboardingService.UserHasBrandAccessAsync(userId, request.BrandProfileId, cancellationToken);
        if (!hasAccess)
        {
            return Result<CampaignResponse>.Failure("You do not have access to this brand profile.");
        }

        var campaign = new MarketingCampaign
        {
            Id = Guid.NewGuid(),
            BrandProfileId = request.BrandProfileId,
            CreatedBy = userId,
            CampaignType = request.CampaignType,
            Status = CampaignStatus.Draft,
            CurrentStep = 1,
            TargetPlatforms = [],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await onboardingService.CreateDraftAsync(campaign, cancellationToken);

        return Result<CampaignResponse>.Success(CampaignResponse.FromEntity(campaign));
    }
}
