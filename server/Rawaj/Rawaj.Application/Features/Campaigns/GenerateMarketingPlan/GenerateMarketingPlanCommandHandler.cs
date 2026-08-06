using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.AiPipeline.Common;
using Rawaj.Application.Features.AiPipeline.Services;

namespace Rawaj.Application.Features.Campaigns.GenerateMarketingPlan;

/// <summary>
/// C19 cutover: a thin shim onto the pipeline orchestrator, request/response contract unchanged.
/// <c>AiPlanJson</c> comes from <c>StrategyAssemble</c> completing — free-trial-first-campaign
/// pricing (<c>Tenant.FreeMarketingPlanUsed</c>) and the "Marketing plan ready" notification are both
/// still applied, now from <c>AiPipelineCoinPolicy</c>/<c>PipelineOrchestrator</c> rather than here.
/// </summary>
public class GenerateMarketingPlanCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IPipelineOrchestrator orchestrator)
    : IRequestHandler<GenerateMarketingPlanCommand, Result<GenerateMarketingPlanResponse>>
{
    public async Task<Result<GenerateMarketingPlanResponse>> Handle(
        GenerateMarketingPlanCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;
        var role = currentTenantContext.Role!.Value;

        var campaign = await dbContext.MarketingCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (campaign is null)
        {
            return Result<GenerateMarketingPlanResponse>.Failure("Campaign not found.");
        }

        var brand = await dbContext.TenantBrandProfiles.FirstAsync(b => b.Id == campaign.BrandProfileId, cancellationToken);

        var run = await LegacyPipelineGateway.EnsureRunAdvancedAsync(
            dbContext, orchestrator, campaign, brand, userId, role, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (campaign.AiPlanJson is null)
        {
            var blockingError = await LegacyPipelineGateway.BlockingErrorAsync(dbContext, run.Id, cancellationToken);
            return Result<GenerateMarketingPlanResponse>.Failure(
                blockingError ?? "Marketing plan generation failed. Please try again.");
        }

        return Result<GenerateMarketingPlanResponse>.Success(
            new GenerateMarketingPlanResponse(campaign.Id, campaign.AiPlanJson, campaign.AiGeneratedAt ?? DateTime.UtcNow));
    }
}
