using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.AiPipeline.Services;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.CancelRun;

public class CancelRunCommandHandler(
    IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext, IPipelineOrchestrator orchestrator)
    : IRequestHandler<CancelRunCommand, Result<CancelRunResponse>>
{
    public async Task<Result<CancelRunResponse>> Handle(CancelRunCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var run = await dbContext.AiPipelineRuns
            .FirstOrDefaultAsync(r => r.Id == request.RunId && r.TenantId == tenantId, cancellationToken);
        if (run is null)
        {
            return Result<CancelRunResponse>.Failure("Pipeline run not found.");
        }

        if (run.Status is AiPipelineRunStatus.Completed or AiPipelineRunStatus.Cancelled or AiPipelineRunStatus.Failed)
        {
            return Result<CancelRunResponse>.Failure($"This run is already {run.Status} and cannot be cancelled.");
        }

        await orchestrator.CancelAsync(run, cancellationToken);

        return Result<CancelRunResponse>.Success(new CancelRunResponse(run.Id, run.Status));
    }
}
