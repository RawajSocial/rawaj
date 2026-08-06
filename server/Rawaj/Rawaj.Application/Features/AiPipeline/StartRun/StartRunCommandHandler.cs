using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.AiPipeline.Services;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.StartRun;

/// <summary>
/// Starts a pipeline run and returns immediately with its id — the worker
/// (<c>AiPipelineWorkerHostedService</c>) picks it up on its next poll and does the actual advancing,
/// so this handler never blocks a request on a provider call.
/// </summary>
public class StartRunCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IPipelineOrchestrator orchestrator)
    : IRequestHandler<StartRunCommand, Result<StartRunResponse>>
{
    public async Task<Result<StartRunResponse>> Handle(StartRunCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;

        var campaign = await dbContext.MarketingCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (campaign is null)
        {
            return Result<StartRunResponse>.Failure("Campaign not found.");
        }

        // One active run per campaign at a time — a second would race the first for the same
        // artifacts and the same coin charges.
        var hasActiveRun = await dbContext.AiPipelineRuns.AnyAsync(
            r => r.CampaignId == campaign.Id
                 && r.Status != AiPipelineRunStatus.Completed
                 && r.Status != AiPipelineRunStatus.Failed
                 && r.Status != AiPipelineRunStatus.Cancelled,
            cancellationToken);

        if (hasActiveRun)
        {
            return Result<StartRunResponse>.Failure("A pipeline run is already in progress for this campaign.");
        }

        var run = await orchestrator.StartAsync(tenantId, campaign.BrandProfileId, campaign.Id, userId, cancellationToken);

        campaign.CurrentPipelineRunId = run.Id;
        campaign.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<StartRunResponse>.Success(new StartRunResponse(run.Id, run.Status));
    }
}
