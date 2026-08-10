using Rawaj.Application.Features.Dashboard.GetDashboardOverview;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.SocialMedia;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Persistence;
using Xunit;

namespace Rawaj.Application.Tests.Features.Dashboard.GetDashboardOverview;

/// <summary>
/// Phase 6: DashboardOverviewResponse gains a Bottom Posts surface and the *Available flag pattern
/// already present on CampaignAnalyticsSummary, matching GetBrandAnalyticsQueryHandler's equivalent
/// Phase 6 additions.
/// </summary>
public class GetDashboardOverviewQueryHandlerTests
{
    private static async Task<(AppDbContext DbContext, Guid BrandProfileId)> SeedBrandWithPostsAsync(
        params (long? Views, long? UniqueViewers, int? Likes, int? Comments, int? Shares)[] posts)
    {
        var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var brandProfileId = Guid.NewGuid();
        var socialAccountId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        dbContext.TenantBrandProfiles.Add(new TenantBrandProfile
        {
            Id = brandProfileId,
            TenantId = tenantId,
            Name = "Test Brand",
            Status = BrandProfileStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        });

        dbContext.SocialAccounts.Add(new SocialAccount
        {
            Id = socialAccountId,
            BrandProfileId = brandProfileId,
            Platform = SocialPlatform.Facebook,
            AccountName = "Test Page",
            AccountIdExternal = "123",
            Token = "encrypted-token",
            IsActive = true,
            CreatedAt = now
        });

        foreach (var post in posts)
        {
            var contentItemId = Guid.NewGuid();
            var scheduledPostId = Guid.NewGuid();

            dbContext.ContentItems.Add(new ContentItem
            {
                Id = contentItemId,
                TenantId = tenantId,
                BrandProfileId = brandProfileId,
                CreatedBy = Guid.NewGuid(),
                ContentType = ContentType.Post,
                Platform = SocialPlatform.Facebook,
                Language = Language.En,
                Content = "Post body",
                Status = ContentStatus.Approved,
                CreatedAt = now,
                UpdatedAt = now,
                RowVersion = new byte[8]
            });

            dbContext.ScheduledPosts.Add(new ScheduledPost
            {
                Id = scheduledPostId,
                ContentItemId = contentItemId,
                SocialAccountId = socialAccountId,
                BrandProfileId = brandProfileId,
                ScheduledAt = now,
                Status = ScheduledPostStatus.Published,
                PostId = $"meta-post-{scheduledPostId:N}",
                PublishedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            });

            dbContext.PostAnalytics.Add(new PostAnalytics
            {
                Id = Guid.NewGuid(),
                ScheduledPostId = scheduledPostId,
                Platform = SocialPlatform.Facebook,
                RecordedAt = now,
                Views = post.Views,
                UniqueViewers = post.UniqueViewers,
                Likes = post.Likes,
                Comments = post.Comments,
                Shares = post.Shares
            });
        }

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return (dbContext, brandProfileId);
    }

    [Fact]
    public async Task Handle_BottomPosts_RanksAscendingByUniqueViewers()
    {
        var (dbContext, brandProfileId) = await SeedBrandWithPostsAsync(
            (Views: 10, UniqueViewers: 5, Likes: 1, Comments: 0, Shares: 0),
            (Views: 300, UniqueViewers: 250, Likes: 10, Comments: 2, Shares: 1));
        var handler = new GetDashboardOverviewQueryHandler(dbContext);

        var result = await handler.Handle(new GetDashboardOverviewQuery(brandProfileId, null), CancellationToken.None);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Equal(5, result.Data!.BottomPosts[0].UniqueViewers);
        Assert.Equal(250, result.Data!.TopPosts[0].UniqueViewers);
    }

    [Fact]
    public async Task Handle_AvailabilityFlags_FalseWhenNoPostHasViews()
    {
        var (dbContext, brandProfileId) = await SeedBrandWithPostsAsync(
            (Views: null, UniqueViewers: null, Likes: 5, Comments: 0, Shares: 0));
        var handler = new GetDashboardOverviewQueryHandler(dbContext);

        var result = await handler.Handle(new GetDashboardOverviewQuery(brandProfileId, null), CancellationToken.None);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.False(result.Data!.ViewsAvailable);
        Assert.False(result.Data!.UniqueViewersAvailable);
        Assert.False(result.Data!.EngagementRateAvailable);
    }

    [Fact]
    public async Task Handle_ChangePercents_NullWhenAllDataIsFromThisMonth()
    {
        var (dbContext, brandProfileId) = await SeedBrandWithPostsAsync(
            (Views: 10, UniqueViewers: 2, Likes: 3, Comments: 1, Shares: 0));
        var handler = new GetDashboardOverviewQueryHandler(dbContext);

        var result = await handler.Handle(new GetDashboardOverviewQuery(brandProfileId, null), CancellationToken.None);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Null(result.Data!.PostsTrackedChangePercent);
        Assert.Null(result.Data!.TotalUniqueViewersChangePercent);
        Assert.Null(result.Data!.AverageEngagementRateChangePercent);
        Assert.Null(result.Data!.TotalFollowersChangePercent);
    }

    [Fact]
    public async Task Handle_ChangePercents_ComputedCorrectlyWhenPriorMonthDataExists()
    {
        var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var brandProfileId = Guid.NewGuid();
        var socialAccountId = Guid.NewGuid();
        var contentItemId = Guid.NewGuid();
        var scheduledPostId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        // Safely in the previous calendar month regardless of when this test runs.
        var lastMonth = monthStart.AddDays(-5);

        dbContext.TenantBrandProfiles.Add(new TenantBrandProfile
        {
            Id = brandProfileId, TenantId = tenantId, Name = "Test Brand",
            Status = BrandProfileStatus.Active, CreatedAt = lastMonth, UpdatedAt = now
        });

        dbContext.SocialAccounts.Add(new SocialAccount
        {
            Id = socialAccountId, BrandProfileId = brandProfileId, Platform = SocialPlatform.Facebook,
            AccountName = "Test Page", AccountIdExternal = "123", Token = "encrypted-token",
            IsActive = true, CreatedAt = lastMonth, FollowerCount = 150
        });

        // Last month's follower snapshot (150 now, 100 back then -> +50%).
        dbContext.FollowerCountSnapshots.Add(new FollowerCountSnapshot
        {
            Id = Guid.NewGuid(), SocialAccountId = socialAccountId, Platform = SocialPlatform.Facebook,
            RecordedAt = lastMonth, FollowerCount = 100
        });

        dbContext.ContentItems.Add(new ContentItem
        {
            Id = contentItemId, TenantId = tenantId, BrandProfileId = brandProfileId, CreatedBy = Guid.NewGuid(),
            ContentType = ContentType.Post, Platform = SocialPlatform.Facebook, Language = Language.En,
            Content = "Post body", Status = ContentStatus.Approved, CreatedAt = lastMonth, UpdatedAt = now,
            RowVersion = new byte[8]
        });

        dbContext.ScheduledPosts.Add(new ScheduledPost
        {
            Id = scheduledPostId, ContentItemId = contentItemId, SocialAccountId = socialAccountId,
            BrandProfileId = brandProfileId, ScheduledAt = lastMonth, Status = ScheduledPostStatus.Published,
            PostId = $"meta-post-{scheduledPostId:N}", PublishedAt = lastMonth, CreatedAt = lastMonth, UpdatedAt = now
        });

        // Same single post, two snapshots: last month's (10 unique viewers, 100 views, 20 likes ->
        // 0.2 engagement rate) and this month's (20 unique viewers, 200 views, 10 likes -> 0.05).
        dbContext.PostAnalytics.Add(new PostAnalytics
        {
            Id = Guid.NewGuid(), ScheduledPostId = scheduledPostId, Platform = SocialPlatform.Facebook,
            RecordedAt = lastMonth, Views = 100, UniqueViewers = 10, Likes = 20, Comments = 0, Shares = 0
        });
        dbContext.PostAnalytics.Add(new PostAnalytics
        {
            Id = Guid.NewGuid(), ScheduledPostId = scheduledPostId, Platform = SocialPlatform.Facebook,
            RecordedAt = now, Views = 200, UniqueViewers = 20, Likes = 10, Comments = 0, Shares = 0
        });

        await dbContext.SaveChangesAsync(CancellationToken.None);
        var handler = new GetDashboardOverviewQueryHandler(dbContext);

        var result = await handler.Handle(new GetDashboardOverviewQuery(brandProfileId, null), CancellationToken.None);

        Assert.True(result.Succeeded, result.ErrorMessage);
        // Same single post existed both before and after the cutoff -> count unchanged -> a real 0%,
        // not null (distinct from "no prior data").
        Assert.Equal(0m, result.Data!.PostsTrackedChangePercent);
        // Unique viewers: 10 -> 20 = +100%.
        Assert.Equal(100m, result.Data!.TotalUniqueViewersChangePercent);
        // Engagement rate: 0.2 -> 0.05 = -75%.
        Assert.Equal(-75m, result.Data!.AverageEngagementRateChangePercent);
        // Followers: 100 -> 150 = +50%.
        Assert.Equal(50m, result.Data!.TotalFollowersChangePercent);
    }
}
