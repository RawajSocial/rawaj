using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.Services;

/// <summary>
/// Approves the strategy a pipeline run produced — the graph's <c>HumanApproval</c> node is
/// <c>RequiresHuman</c>, so nothing ever dispatches it to a worker; this is what completes it.
///
/// <para>Stamps the exact artifact version, not just a timestamp. Approval used to be a bare
/// <c>PlanApprovedAt</c> against a plan column that refinement overwrote in place, so "which strategy
/// did the user actually approve" had no durable answer once it had been refined again.
/// <see cref="MarketingCampaign.ApprovedStrategyArtifactId"/> is the fix: content generation, and any
/// later audit, can point at exactly what was reviewed rather than merely the most recent thing in
/// the column.</para>
///
/// <para>Does not save — same convention as <see cref="IPipelineArtifactStore"/>: the caller commits,
/// so the stage transition and the campaign's new fields land in one transaction.</para>
/// </summary>
public class PipelineApprovalService(IApplicationDbContext dbContext) : IPipelineApprovalService
{
    public async Task<Result<AiArtifact>> ApproveAsync(
        AiPipelineRun run, MarketingCampaign campaign, CancellationToken cancellationToken)
    {
        var stages = await dbContext.AiPipelineStages
            .Where(s => s.RunId == run.Id)
            .ToListAsync(cancellationToken);

        var approvalStage = stages.FirstOrDefault(s => s.Kind == AiPipelineStageKind.HumanApproval);

        if (approvalStage is null || approvalStage.Status != AiPipelineStageStatus.AwaitingApproval)
        {
            return Result<AiArtifact>.Failure("This run has no strategy currently awaiting approval.");
        }

        var artifact = await dbContext.AiArtifacts
            .Where(a => a.CampaignId == campaign.Id && a.Kind == AiArtifactKind.Strategy && a.IsCurrent)
            .FirstOrDefaultAsync(cancellationToken);

        if (artifact is null)
        {
            return Result<AiArtifact>.Failure("Generate a strategy before approving it.");
        }

        var now = DateTime.UtcNow;

        approvalStage.Status = AiPipelineStageStatus.Completed;
        approvalStage.ArtifactId = artifact.Id;
        approvalStage.CompletedAt = now;

        campaign.ApprovedStrategyArtifactId = artifact.Id;
        campaign.PlanApprovedAt = now;
        if (campaign.Status == CampaignStatus.Draft)
        {
            campaign.Status = CampaignStatus.Active;
        }
        campaign.UpdatedAt = now;

        // Recomputed from the stages, not hardcoded to Running — a run whose only remaining stages
        // are optional and already Skipped is Completed the moment approval clears, not merely
        // resumed.
        run.Status = AiPipelinePolicy.EvaluateRunStatus(stages);
        run.UpdatedAt = now;

        return Result<AiArtifact>.Success(artifact);
    }
}
