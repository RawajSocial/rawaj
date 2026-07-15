using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Analytics.GetPostAnalytics;

public class GetPostAnalyticsQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetPostAnalyticsQuery, Result<List<PostAnalyticsSnapshot>>>
{
    public async Task<Result<List<PostAnalyticsSnapshot>>> Handle(GetPostAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var belongsToTenant = await dbContext.ScheduledPosts.AnyAsync(
            s => s.Id == request.ScheduledPostId && s.ContentItem.TenantId == tenantId, cancellationToken);
        if (!belongsToTenant)
        {
            return Result<List<PostAnalyticsSnapshot>>.Failure("Scheduled post not found.");
        }

        var snapshots = await dbContext.PostAnalytics
            .Where(a => a.ScheduledPostId == request.ScheduledPostId)
            .OrderByDescending(a => a.RecordedAt)
            .Select(a => new PostAnalyticsSnapshot(
                a.Id,
                a.ScheduledPostId,
                a.Platform,
                a.RecordedAt,
                a.Impressions,
                a.Reach,
                a.Likes,
                a.Comments,
                a.Shares,
                a.Saves,
                a.Clicks,
                a.EngagementRate))
            .ToListAsync(cancellationToken);

        return Result<List<PostAnalyticsSnapshot>>.Success(snapshots);
    }
}
