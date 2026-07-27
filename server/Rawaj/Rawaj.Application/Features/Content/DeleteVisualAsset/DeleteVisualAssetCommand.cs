using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.DeleteVisualAsset;

public record DeleteVisualAssetCommand(Guid VisualAssetId)
    : IRequest<Result<bool>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.VisualAssets.Where(v => v.Id == VisualAssetId).Select(v => v.BrandProfileId).FirstOrDefaultAsync(cancellationToken);
}
