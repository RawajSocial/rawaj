using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.DeleteContentItem;

public record DeleteContentItemCommand(Guid ContentItemId)
    : IRequest<Result<bool>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    // Same gating as ReviewContentItem — an Editor is trusted to accept/reject content, so an
    // Editor is trusted to remove a draft/rejected one too.
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.ContentItems.Where(c => c.Id == ContentItemId).Select(c => (Guid?)c.BrandProfileId).FirstOrDefaultAsync(cancellationToken);
}
