using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.CreatePostFromVisualAsset;

/// <summary>
/// Wraps a standalone, already-generated <c>VisualAsset</c> (one with no linked <c>ContentItem</c>
/// — the content-gen page's "إعلان ثابت" image-only generation) in a lightweight, pre-approved
/// <c>ContentItem</c> so it becomes schedulable through the exact same <c>SchedulePostCommand</c>
/// campaign posts already use. Only needed because scheduling requires a <c>ContentItemId</c>;
/// nothing else about the scheduling/publishing pipeline needs to change.
/// </summary>
public record CreatePostFromVisualAssetCommand(Guid VisualAssetId, SocialPlatform Platform, Language Language)
    : IRequest<Result<CreatePostFromVisualAssetResponse>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.VisualAssets.Where(v => v.Id == VisualAssetId).Select(v => v.BrandProfileId).FirstOrDefaultAsync(cancellationToken);
}
