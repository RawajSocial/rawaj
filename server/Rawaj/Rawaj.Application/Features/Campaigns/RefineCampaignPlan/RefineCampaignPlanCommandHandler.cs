using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.Content.Common;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.RefineCampaignPlan;

/// <summary>
/// Lets the user nudge an already-generated campaign strategy by free text (the "عدّل الخطة" box)
/// before approving it — mirrors RegenerateContentItemCommandHandler's feedback-driven revision,
/// but for the whole strategy JSON rather than a single content item. Charges AI Reasoning
/// Conversation, the pricing-sheet line item for exactly this kind of iterative back-and-forth.
/// </summary>
public class RefineCampaignPlanCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IAiTextGenerationService textGenerationService,
    ICoinCostProvider coinCostProvider)
    : IRequestHandler<RefineCampaignPlanCommand, Result<RefineCampaignPlanResponse>>
{
    public async Task<Result<RefineCampaignPlanResponse>> Handle(
        RefineCampaignPlanCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;
        var role = currentTenantContext.Role!.Value;

        var campaign = await dbContext.MarketingCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (campaign is null)
        {
            return Result<RefineCampaignPlanResponse>.Failure("Campaign not found.");
        }

        if (string.IsNullOrWhiteSpace(campaign.AiPlanJson))
        {
            return Result<RefineCampaignPlanResponse>.Failure("Generate a strategy before refining it.");
        }

        if (campaign.PlanApprovedAt is not null)
        {
            return Result<RefineCampaignPlanResponse>.Failure("This campaign's strategy is already approved and can no longer be refined.");
        }

        var brand = await dbContext.TenantBrandProfiles.FirstAsync(b => b.Id == campaign.BrandProfileId, cancellationToken);

        var coinCost = await CoinPricingPolicy.GetDiscountedCostAsync(dbContext, tenantId, coinCostProvider.ReasoningConversation, cancellationToken);
        var coinBalance = await CoinPolicy.GetBalanceAsync(dbContext, tenantId, userId, role, cancellationToken);
        if (coinBalance < coinCost)
        {
            return Result<RefineCampaignPlanResponse>.Failure(
                CoinPolicy.InsufficientCoinsMessage(coinCost, coinBalance, "refine the strategy"));
        }

        var prompt = ContentPromptBuilder.BuildPlanRefinementPrompt(brand, campaign, campaign.AiPlanJson, request.Feedback);
        var generation = await textGenerationService.GenerateTextAsync(prompt, cancellationToken);

        var now = DateTime.UtcNow;

        var job = new AiJob
        {
            Id = Guid.NewGuid(),
            BrandProfileId = brand.Id,
            TriggeredBy = userId,
            JobType = AiJobType.PlanGeneration,
            Status = generation.Succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,
            InputParams = JsonSerializer.Serialize(new { prompt }),
            OutputRefId = generation.Succeeded ? campaign.Id : null,
            OutputRefType = "marketing_campaign",
            Tokens = generation.TokensUsed,
            ErrorMessage = generation.ErrorMessage,
            StartedAt = now,
            CompletedAt = now,
            CreatedAt = now
        };
        dbContext.AiJobs.Add(job);

        if (!generation.Succeeded)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<RefineCampaignPlanResponse>.Failure(
                generation.ErrorMessage ?? "Strategy refinement failed. Please try again.");
        }

        campaign.AiPlanJson = generation.Text!;
        campaign.AiGeneratedAt = now;
        campaign.UpdatedAt = now;

        await CoinPolicy.TrySpendAsync(dbContext, tenantId, userId, role, coinCost, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<RefineCampaignPlanResponse>.Success(
            new RefineCampaignPlanResponse(campaign.Id, campaign.AiPlanJson, now));
    }
}
