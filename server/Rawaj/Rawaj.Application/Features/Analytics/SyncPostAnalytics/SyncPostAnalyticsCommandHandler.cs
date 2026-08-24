using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Analytics.Common;

namespace Rawaj.Application.Features.Analytics.SyncPostAnalytics;

/// <summary>
/// Fetches current metrics from the platform for an already-published post and records a new
/// snapshot row. Views/Unique Viewers require the read_insights permission (Advanced Access
/// review) and stay null for accounts that haven't (re)connected with that scope granted -
/// engagement counts (likes/comments/shares) only need the same page token already used to
/// publish, and are unaffected by that.
/// </summary>
public class SyncPostAnalyticsCommandHandler(
    IApplicationDbContext dbContext,
    ITokenEncryptor tokenEncryptor,
    ICurrentTenantContext currentTenantContext,
    IEnumerable<ISocialAnalyticsProvider> analyticsProviders)
    : IRequestHandler<SyncPostAnalyticsCommand, Result<PostAnalyticsSnapshot>>
{
    public async Task<Result<PostAnalyticsSnapshot>> Handle(SyncPostAnalyticsCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var scheduledPost = await dbContext.ScheduledPosts
            .FirstOrDefaultAsync(s => s.Id == request.ScheduledPostId && s.ContentItem.TenantId == tenantId, cancellationToken);
        if (scheduledPost is null)
        {
            return Result<PostAnalyticsSnapshot>.Failure("Scheduled post not found.");
        }

        var syncOutcome = await PostAnalyticsSyncer.SyncOneAsync(
            dbContext, tokenEncryptor, analyticsProviders, scheduledPost, cancellationToken);
        if (!syncOutcome.Succeeded)
        {
            return Result<PostAnalyticsSnapshot>.Failure(syncOutcome.ErrorMessage ?? "Failed to fetch analytics.");
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var analytics = syncOutcome.Analytics!;
        return Result<PostAnalyticsSnapshot>.Success(new PostAnalyticsSnapshot(
            analytics.Id,
            analytics.ScheduledPostId,
            analytics.Platform,
            analytics.RecordedAt,
            analytics.Views,
            analytics.UniqueViewers,
            analytics.Likes,
            analytics.Comments,
            analytics.Shares,
            analytics.Saves,
            analytics.Clicks,
            analytics.EngagementRate));
    }
}
