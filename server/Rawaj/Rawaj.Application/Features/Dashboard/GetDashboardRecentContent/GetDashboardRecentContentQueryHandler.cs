using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Content.GetContentItems;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Dashboard.GetDashboardRecentContent;

/// <summary>
/// A dashboard widget for "what just went out", not "what was just generated" - scoped to
/// Published content only and ordered by when it actually went live (ScheduledPost.PublishedAt),
/// not ContentItem.CreatedAt. Ordering by generation time let a published post fall out of the
/// fixed-size Take(n) window whenever enough newer drafts/regenerations (of any status) were
/// created after it, since generation time and publish time are decoupled - a post can sit in
/// Draft/Approved for a while before actually being scheduled and going live.
/// </summary>
public class GetDashboardRecentContentQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetDashboardRecentContentQuery, Result<List<ContentItemSummary>>>
{
    public async Task<Result<List<ContentItemSummary>>> Handle(GetDashboardRecentContentQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var take = Math.Clamp(request.Take, 1, 50);

        var items = await dbContext.ContentItems
            .Where(c => c.BrandProfileId == request.BrandProfileId && c.TenantId == tenantId)
            .Where(c => request.CampaignId == null || c.CampaignId == request.CampaignId)
            .Where(c => c.Status == ContentStatus.Published)
            .OrderByDescending(c => dbContext.ScheduledPosts
                .Where(s => s.ContentItemId == c.Id && s.Status == ScheduledPostStatus.Published)
                .OrderByDescending(s => s.PublishedAt)
                .Select(s => s.PublishedAt)
                .FirstOrDefault() ?? c.UpdatedAt)
            .Take(take)
            .Select(c => new ContentItemSummary(
                c.Id, c.ContentType, c.Platform, c.Language, c.Content, c.Status, c.CreatedAt, c.SuggestedPostAt,
                c.VisualAssets
                    .OrderByDescending(v => v.CreatedAt)
                    .Select(v => (Guid?)v.Id)
                    .FirstOrDefault(),
                c.VisualAssets
                    .OrderByDescending(v => v.CreatedAt)
                    .Select(v => v.FileUrl)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return Result<List<ContentItemSummary>>.Success(items);
    }
}
