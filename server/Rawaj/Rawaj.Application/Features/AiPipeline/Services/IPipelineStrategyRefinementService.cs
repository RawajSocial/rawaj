using Rawaj.Application.Common.Models;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;

namespace Rawaj.Application.Features.AiPipeline.Services;

/// <summary>
/// Refines the current strategy artifact by free-text feedback. See
/// <see cref="PipelineStrategyRefinementService"/>.
/// </summary>
public interface IPipelineStrategyRefinementService
{
    Task<Result<AiArtifact>> RefineAsync(
        TenantBrandProfile brand,
        MarketingCampaign campaign,
        Guid userId,
        string feedback,
        CancellationToken cancellationToken);
}
