using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.ResumeRun;

/// <summary>Advances a run right now instead of waiting for the worker's next poll — for after a
/// coin top-up, or simply so the UI doesn't sit for up to the poll interval doing nothing visible.</summary>
public record ResumeRunCommand(Guid RunId)
    : IRequest<Result<ResumeRunResponse>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.AiPipelineRuns.Where(r => r.Id == RunId).Select(r => (Guid?)r.BrandProfileId).FirstOrDefaultAsync(cancellationToken);
}
