using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.RunStage;

/// <summary>Forces one stage to retry right now, bypassing its backoff — "retry this step" on a
/// specific failed or skipped stage, without waiting for <c>NextAttemptAt</c> to pass on its own.</summary>
public record RunStageCommand(Guid RunId, AiPipelineStageKind Kind)
    : IRequest<Result<RunStageResponse>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.AiPipelineRuns.Where(r => r.Id == RunId).Select(r => (Guid?)r.BrandProfileId).FirstOrDefaultAsync(cancellationToken);
}
