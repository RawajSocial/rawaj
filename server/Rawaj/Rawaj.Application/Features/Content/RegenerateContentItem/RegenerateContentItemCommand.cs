using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.RegenerateContentItem;

public record RegenerateContentItemCommand(Guid ContentItemId, string Feedback)
    : IRequest<Result<RegenerateContentItemResponse>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.ContentItems.Where(c => c.Id == ContentItemId).Select(c => c.BrandProfileId).FirstOrDefaultAsync(cancellationToken);
}
