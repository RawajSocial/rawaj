using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.AiPipeline.Services;

namespace Rawaj.Application.Features.AiPipeline.RefineStrategy;

/// <summary>
/// The pipeline-native replacement for <c>RefineCampaignPlanCommandHandler</c>: coins are still
/// checked and charged at this layer (the caller), matching how every executor leaves charging to
/// <c>AiPipelineCoinPolicy</c> rather than charging itself — <see cref="IPipelineStrategyRefinementService"/>
/// only produces the new version.
/// </summary>
public class RefineStrategyCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IPipelineStrategyRefinementService refinementService,
    ICoinCostProvider coinCostProvider)
    : IRequestHandler<RefineStrategyCommand, Result<RefineStrategyResponse>>
{
    public async Task<Result<RefineStrategyResponse>> Handle(RefineStrategyCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;
        var role = currentTenantContext.Role!.Value;

        var campaign = await dbContext.MarketingCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (campaign is null)
        {
            return Result<RefineStrategyResponse>.Failure("Campaign not found.");
        }

        var brand = await dbContext.TenantBrandProfiles.FirstAsync(b => b.Id == campaign.BrandProfileId, cancellationToken);

        var coinCost = await CoinPricingPolicy.GetDiscountedCostAsync(
            dbContext, tenantId, coinCostProvider.ReasoningConversation, cancellationToken);
        var coinBalance = await CoinPolicy.GetBalanceAsync(dbContext, tenantId, userId, role, cancellationToken);
        if (coinBalance < coinCost)
        {
            return Result<RefineStrategyResponse>.Failure(
                CoinPolicy.InsufficientCoinsMessage(coinCost, coinBalance, "refine the strategy"));
        }

        var refined = await refinementService.RefineAsync(brand, campaign, userId, request.Feedback, cancellationToken);
        if (!refined.Succeeded)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<RefineStrategyResponse>.Failure(refined.ErrorMessage!);
        }

        await CoinPolicy.TrySpendAsync(
            dbContext, tenantId, userId, role, coinCost, cancellationToken, reason: "refine_campaign_plan",
            referenceId: campaign.Id, referenceType: "marketing_campaign");

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<RefineStrategyResponse>.Success(
            new RefineStrategyResponse(refined.Data!.Id, refined.Data.Version, refined.Data.ContentJson));
    }
}
