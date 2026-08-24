using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.AiPipeline.Services;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.ApproveStrategy;

/// <summary>
/// Approves the run's current strategy via <see cref="IPipelineApprovalService"/>, then immediately
/// advances the run once — content generation can start right away instead of waiting for the
/// worker's next poll.
/// </summary>
public class ApproveStrategyCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentTenantContext currentTenantContext,
    IPipelineApprovalService approvalService,
    IPipelineOrchestrator orchestrator)
    : IRequestHandler<ApproveStrategyCommand, Result<ApproveStrategyResponse>>
{
    public async Task<Result<ApproveStrategyResponse>> Handle(ApproveStrategyCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var role = currentTenantContext.Role!.Value;

        var run = await dbContext.AiPipelineRuns
            .FirstOrDefaultAsync(r => r.Id == request.RunId && r.TenantId == tenantId, cancellationToken);
        if (run is null || run.CampaignId is null)
        {
            return Result<ApproveStrategyResponse>.Failure("Pipeline run not found.");
        }

        var campaign = await dbContext.MarketingCampaigns.FirstAsync(c => c.Id == run.CampaignId, cancellationToken);

        var approved = await approvalService.ApproveAsync(run, campaign, cancellationToken);
        if (!approved.Succeeded)
        {
            return Result<ApproveStrategyResponse>.Failure(approved.ErrorMessage!);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await orchestrator.AdvanceAsync(run, role, cancellationToken);

        return Result<ApproveStrategyResponse>.Success(
            new ApproveStrategyResponse(run.Id, approved.Data!.Id, campaign.PlanApprovedAt!.Value));
    }
}
