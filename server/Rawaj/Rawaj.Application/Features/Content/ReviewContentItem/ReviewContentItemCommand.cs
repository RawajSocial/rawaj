using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.ReviewContentItem;

public record ReviewContentItemCommand(Guid ContentItemId, bool Approve)
    : IRequest<Result<ReviewContentItemResponse>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    // Dropped from Admin to Editor and given a brand-access check — every other content command
    // (generate/regenerate/schedule) already gates this way, and accept/refuse is exactly the kind
    // of everyday review action an Editor is meant to be able to do without an Admin's involvement.
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.ContentItems.Where(c => c.Id == ContentItemId).Select(c => (Guid?)c.BrandProfileId).FirstOrDefaultAsync(cancellationToken);
}
