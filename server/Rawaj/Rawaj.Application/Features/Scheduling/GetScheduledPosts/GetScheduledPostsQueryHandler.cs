using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Analytics.Common;

namespace Rawaj.Application.Features.Scheduling.GetScheduledPosts;

/// <summary>
/// Backs Calendar and the Ads/My Media pages. BrandProfileId/CampaignId are now denormalized
/// directly onto ScheduledPost, so no more join-based resolution through ContentItem/SocialAccount
/// is needed to scope the query.
/// </summary>
public class GetScheduledPostsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetScheduledPostsQuery, Result<PagedResult<ScheduledPostSummary>>>
{
    public async Task<Result<PagedResult<ScheduledPostSummary>>> Handle(GetScheduledPostsQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = PaginationDefaults.Clamp(request.Page, request.PageSize);

        var query = dbContext.ScheduledPosts
            .Where(s => s.BrandProfileId == request.BrandProfileId)
            .Where(s => request.CampaignId == null || s.CampaignId == request.CampaignId);

        var orderedQuery = query.OrderBy(s => s.ScheduledAt);

        var totalCount = await orderedQuery.CountAsync(cancellationToken);

        var posts = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                s.ContentItemId,
                s.BrandProfileId,
                s.CampaignId,
                s.SocialAccount.Platform,
                s.SocialAccount.AccountName,
                s.ScheduledAt,
                s.Status,
                s.PublishedAt,
                s.ErrorMessage,
                Content = s.ContentItem.Content,
                ImageUrl = s.VisualAsset != null ? s.VisualAsset.FileUrl : null,
            })
            .ToListAsync(cancellationToken);

        var postIds = posts.Select(p => p.Id).ToList();
        var latestAnalyticsPerPost = (await PostAnalyticsAggregation.GetLatestPerPostAsync(
                dbContext.PostAnalytics.Where(a => postIds.Contains(a.ScheduledPostId)),
                cancellationToken))
            .ToDictionary(a => a.ScheduledPostId);

        var summaries = posts
            .Select(p =>
            {
                latestAnalyticsPerPost.TryGetValue(p.Id, out var analytics);
                return new ScheduledPostSummary(
                    p.Id,
                    p.ContentItemId,
                    p.BrandProfileId,
                    p.CampaignId,
                    p.Platform,
                    p.AccountName,
                    p.ScheduledAt,
                    p.Status,
                    p.PublishedAt,
                    p.ErrorMessage,
                    analytics?.Impressions,
                    analytics?.Reach,
                    analytics?.Likes,
                    analytics?.Comments,
                    analytics?.Shares,
                    analytics?.Clicks,
                    analytics?.EngagementRate,
                    p.Content,
                    p.ImageUrl);
            })
            .ToList();

        return Result<PagedResult<ScheduledPostSummary>>.Success(new PagedResult<ScheduledPostSummary>(summaries, page, pageSize, totalCount));
    }
}
