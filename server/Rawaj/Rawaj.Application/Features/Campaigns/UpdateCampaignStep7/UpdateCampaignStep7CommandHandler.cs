using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Campaigns.Common;
using Rawaj.Domain.ValueObjects;

namespace Rawaj.Application.Features.Campaigns.UpdateCampaignStep7;

public class UpdateCampaignStep7CommandHandler(
    ICampaignOnboardingService onboardingService,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateCampaignStep7Command, Result<CampaignResponse>>
{
    public async Task<Result<CampaignResponse>> Handle(UpdateCampaignStep7Command request, CancellationToken cancellationToken)
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

        if (!CampaignOnboardingStepRules.CanApplyStep(campaign.CurrentStep, 7))
        {
            return Result<CampaignResponse>.Failure("Cannot skip ahead. Complete previous steps first.");
        }

        campaign.StrategistAnswers = request.Answers
            .Select(a => new StrategistQA { Question = a.Question, Answer = a.Answer })
            .ToList();

        CampaignOnboardingStepRules.AdvanceStep(campaign, 7);
        campaign.IsOnboardingComplete = true;
        campaign.OnboardingCompletedAt = DateTime.UtcNow;
        campaign.UpdatedAt = DateTime.UtcNow;

        await onboardingService.SaveChangesAsync(cancellationToken);

        return Result<CampaignResponse>.Success(CampaignResponse.FromEntity(campaign));
    }
}
