using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.AiPipeline.Services;

namespace Rawaj.Application.Features.Campaigns.RefineCampaignPlan;

/// <summary>
/// C19 cutover: a thin shim onto <see cref="IPipelineStrategyRefinementService"/> — the pipeline-native
/// replacement this command handed off to as of C13/C18, request/response contract unchanged. Coins
/// are still checked and charged here (the caller), matching every other pipeline entry point; the
/// service only produces the new version and writes it through to <c>AiPlanJson</c>.
/// </summary>
public class RefineCampaignPlanCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IPipelineStrategyRefinementService refinementService,
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

        var brand = await dbContext.TenantBrandProfiles.FirstAsync(b => b.Id == campaign.BrandProfileId, cancellationToken);

        var coinCost = await CoinPricingPolicy.GetDiscountedCostAsync(
            dbContext, tenantId, coinCostProvider.ReasoningConversation, cancellationToken);
        var coinBalance = await CoinPolicy.GetBalanceAsync(dbContext, tenantId, userId, role, cancellationToken);
        if (coinBalance < coinCost)
        {
            return Result<RefineCampaignPlanResponse>.Failure(
                CoinPolicy.InsufficientCoinsMessage(coinCost, coinBalance, "refine the strategy"));
        }

        var refined = await refinementService.RefineAsync(brand, campaign, userId, request.Feedback, cancellationToken);
        if (!refined.Succeeded)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<RefineCampaignPlanResponse>.Failure(refined.ErrorMessage!);
        }

        await CoinPolicy.TrySpendAsync(
            dbContext, tenantId, userId, role, coinCost, cancellationToken, reason: "refine_campaign_plan");

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<RefineCampaignPlanResponse>.Success(
            new RefineCampaignPlanResponse(campaign.Id, campaign.AiPlanJson!, campaign.AiGeneratedAt!.Value));
    }
}
