using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.GetContentRevisions;

public record GetContentRevisionsQuery(Guid ContentItemId)
    : IRequest<Result<List<ContentRevisionSummary>>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.ContentItems.Where(c => c.Id == ContentItemId).Select(c => c.BrandProfileId).FirstOrDefaultAsync(cancellationToken);
}
