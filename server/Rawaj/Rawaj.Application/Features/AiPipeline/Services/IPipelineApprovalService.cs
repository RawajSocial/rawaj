using Rawaj.Application.Common.Models;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Campaigns;

namespace Rawaj.Application.Features.AiPipeline.Services;

/// <summary>
/// Approves the strategy a pipeline run produced. See <see cref="PipelineApprovalService"/>.
/// </summary>
public interface IPipelineApprovalService
{
    Task<Result<AiArtifact>> ApproveAsync(
        AiPipelineRun run, MarketingCampaign campaign, CancellationToken cancellationToken);
}
