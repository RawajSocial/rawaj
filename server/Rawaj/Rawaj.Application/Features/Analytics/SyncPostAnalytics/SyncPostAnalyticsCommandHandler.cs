using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Entities.SocialMedia;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Analytics.SyncPostAnalytics;

/// <summary>
/// Fetches current engagement counts from the platform for an already-published post and records
/// a new snapshot row. Impressions/reach require the read_insights permission (Advanced Access
/// review), so they stay null until that's granted - engagement counts (likes/comments/shares)
/// only need the same page token already used to publish.
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

        if (scheduledPost.Status != ScheduledPostStatus.Published || string.IsNullOrWhiteSpace(scheduledPost.PostId))
        {
            return Result<PostAnalyticsSnapshot>.Failure("Only published posts have analytics to sync.");
        }

        var socialAccount = await dbContext.SocialAccounts
            .FirstAsync(s => s.Id == scheduledPost.SocialAccountId, cancellationToken);

        var provider = analyticsProviders.FirstOrDefault(p => p.Platform == socialAccount.Platform);
        if (provider is null)
        {
            return Result<PostAnalyticsSnapshot>.Failure($"Analytics are not yet supported for {socialAccount.Platform}.");
        }

        var accessToken = tokenEncryptor.Decrypt(socialAccount.Token);
        var metrics = await provider.GetMetricsAsync(scheduledPost.PostId, accessToken, cancellationToken);

        if (!metrics.Succeeded)
        {
            return Result<PostAnalyticsSnapshot>.Failure(metrics.ErrorMessage ?? "Failed to fetch analytics.");
        }

        var engagementRate = metrics.Impressions is > 0
            ? Math.Round((decimal)((metrics.Likes ?? 0) + (metrics.Comments ?? 0) + (metrics.Shares ?? 0)) / metrics.Impressions.Value, 4)
            : (decimal?)null;

        var analytics = new PostAnalytics
        {
            Id = Guid.NewGuid(),
            ScheduledPostId = scheduledPost.Id,
            Platform = socialAccount.Platform,
            RecordedAt = DateTime.UtcNow,
            Impressions = metrics.Impressions,
            Reach = metrics.Reach,
            Likes = metrics.Likes,
            Comments = metrics.Comments,
            Shares = metrics.Shares,
            EngagementRate = engagementRate
        };

        dbContext.PostAnalytics.Add(analytics);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<PostAnalyticsSnapshot>.Success(new PostAnalyticsSnapshot(
            analytics.Id,
            analytics.ScheduledPostId,
            analytics.Platform,
            analytics.RecordedAt,
            analytics.Impressions,
            analytics.Reach,
            analytics.Likes,
            analytics.Comments,
            analytics.Shares,
            analytics.Saves,
            analytics.Clicks,
            analytics.EngagementRate));
    }
}
