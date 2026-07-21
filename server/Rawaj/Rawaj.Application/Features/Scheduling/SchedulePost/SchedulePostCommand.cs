using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.SchedulePost;

public record SchedulePostCommand(
    Guid ContentItemId,
    Guid? VisualAssetId,
    Guid SocialAccountId,
    DateTime ScheduledAt,
    bool AiSuggestedTime = false)
    : IRequest<Result<SchedulePostResponse>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.ContentItems.Where(c => c.Id == ContentItemId).Select(c => c.BrandProfileId).FirstOrDefaultAsync(cancellationToken);
}
