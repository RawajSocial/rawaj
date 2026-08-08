using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;

namespace Rawaj.Application.Features.AiPipeline.GetRunStatus;

/// <summary>
/// What the UI polls for a running pipeline. Progress is derived server-side from the stage rows
/// (<see cref="AiPipelineProgressPolicy"/>) rather than animated client-side, so a page reload
/// mid-generation shows the real state instead of nothing.
/// </summary>
public class GetRunStatusQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetRunStatusQuery, Result<GetRunStatusResponse>>
{
    public async Task<Result<GetRunStatusResponse>> Handle(GetRunStatusQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var run = await dbContext.AiPipelineRuns
            .FirstOrDefaultAsync(r => r.Id == request.RunId && r.TenantId == tenantId, cancellationToken);
        if (run is null)
        {
            return Result<GetRunStatusResponse>.Failure("Pipeline run not found.");
        }

        var stages = await dbContext.AiPipelineStages
            .Where(s => s.RunId == run.Id)
            .OrderBy(s => s.Ordinal)
            .ThenBy(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        var progress = AiPipelineProgressPolicy.Calculate(stages, run.Status);

        var stageSummaries = stages
            .Select(s => new StageStatusSummary(s.Kind, s.Status, s.Attempts, s.MaxAttempts, s.LastError))
            .ToList();

        return Result<GetRunStatusResponse>.Success(
            new GetRunStatusResponse(run.Id, run.Status, progress, run.TotalCoinsSpent, run.LastError, stageSummaries));
    }
}
