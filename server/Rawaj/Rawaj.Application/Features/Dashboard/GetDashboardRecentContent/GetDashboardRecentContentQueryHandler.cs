using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Content.GetContentItems;

namespace Rawaj.Application.Features.Dashboard.GetDashboardRecentContent;

/// <summary>
/// Thin wrapper over the same brand/campaign-scoped ContentItem filter as GetContentItemsQuery,
/// just capped to the most recent few for a dashboard widget instead of paged.
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
            .OrderByDescending(c => c.CreatedAt)
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
