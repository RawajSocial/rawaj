using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.AiPipeline.Services;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.ResumeRun;

public class ResumeRunCommandHandler(
    IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext, IPipelineOrchestrator orchestrator)
    : IRequestHandler<ResumeRunCommand, Result<ResumeRunResponse>>
{
    public async Task<Result<ResumeRunResponse>> Handle(ResumeRunCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var role = currentTenantContext.Role!.Value;

        var run = await dbContext.AiPipelineRuns
            .FirstOrDefaultAsync(r => r.Id == request.RunId && r.TenantId == tenantId, cancellationToken);
        if (run is null)
        {
            return Result<ResumeRunResponse>.Failure("Pipeline run not found.");
        }

        if (run.Status == AiPipelineRunStatus.AwaitingApproval)
        {
            return Result<ResumeRunResponse>.Failure("Approve the strategy before resuming this run.");
        }

        if (run.Status is AiPipelineRunStatus.Completed or AiPipelineRunStatus.Cancelled or AiPipelineRunStatus.Failed)
        {
            return Result<ResumeRunResponse>.Failure($"This run is already {run.Status} and cannot be resumed.");
        }

        await orchestrator.AdvanceAsync(run, role, cancellationToken);

        return Result<ResumeRunResponse>.Success(new ResumeRunResponse(run.Id, run.Status));
    }
}
