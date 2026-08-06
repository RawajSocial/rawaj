using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.AiPipeline.Services;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.RunStage;

/// <summary>
/// Manually retries one stage. Attempts already spent are not reset — a stage that permanently
/// failed or was skipped gets one more shot, not a fresh budget, so this can't be used to bypass
/// <c>MaxAttempts</c> by repeated clicking.
/// </summary>
public class RunStageCommandHandler(
    IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext, IPipelineOrchestrator orchestrator)
    : IRequestHandler<RunStageCommand, Result<RunStageResponse>>
{
    private static readonly AiPipelineStageStatus[] RetryableFrom =
        [AiPipelineStageStatus.Pending, AiPipelineStageStatus.Failed, AiPipelineStageStatus.Skipped];

    public async Task<Result<RunStageResponse>> Handle(RunStageCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var role = currentTenantContext.Role!.Value;

        var run = await dbContext.AiPipelineRuns
            .FirstOrDefaultAsync(r => r.Id == request.RunId && r.TenantId == tenantId, cancellationToken);
        if (run is null)
        {
            return Result<RunStageResponse>.Failure("Pipeline run not found.");
        }

        var stage = await dbContext.AiPipelineStages
            .FirstOrDefaultAsync(s => s.RunId == run.Id && s.Kind == request.Kind, cancellationToken);
        if (stage is null)
        {
            return Result<RunStageResponse>.Failure("This stage does not exist on this run yet.");
        }

        if (!RetryableFrom.Contains(stage.Status))
        {
            return Result<RunStageResponse>.Failure($"This stage is currently {stage.Status} and cannot be retried directly.");
        }

        stage.Status = AiPipelineStageStatus.Pending;
        stage.NextAttemptAt = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        await orchestrator.AdvanceAsync(run, role, cancellationToken);

        var reloadedStage = await dbContext.AiPipelineStages.FirstAsync(s => s.Id == stage.Id, cancellationToken);

        return Result<RunStageResponse>.Success(
            new RunStageResponse(run.Id, run.Status, reloadedStage.Kind, reloadedStage.Status));
    }
}
