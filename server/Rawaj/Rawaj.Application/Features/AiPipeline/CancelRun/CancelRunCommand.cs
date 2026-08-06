using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.CancelRun;

public record CancelRunCommand(Guid RunId)
    : IRequest<Result<CancelRunResponse>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.AiPipelineRuns.Where(r => r.Id == RunId).Select(r => (Guid?)r.BrandProfileId).FirstOrDefaultAsync(cancellationToken);
}
