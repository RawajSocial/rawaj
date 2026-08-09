using Rawaj.Application.Features.Dashboard.GetDashboardCharts;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.SocialMedia;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Persistence;
using Xunit;

namespace Rawaj.Application.Tests.Features.Dashboard.GetDashboardCharts;

/// <summary>
/// Phase 5: GetDashboardChartsQueryHandler's per-day EngagementRate bucket had the same unweighted-
/// average shape as the Phase 4 rollup bug (averaging each snapshot's own EngagementRate instead of
/// SUM(Engagements)/SUM(Views) for the day). These tests cover the fix, plus confirm the handler's
/// output uses Views/UniqueViewers exclusively.
/// </summary>
public class GetDashboardChartsQueryHandlerTests
{
    private static async Task<(AppDbContext DbContext, Guid BrandProfileId, Guid SocialAccountId, Guid ContentItemId)> SeedBrandAsync()
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

        var contentItemId = Guid.NewGuid();
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

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return (dbContext, brandProfileId, socialAccountId, contentItemId);
    }

    private static Guid AddScheduledPost(AppDbContext dbContext, Guid brandProfileId, Guid socialAccountId, Guid contentItemId)
    {
        var scheduledPostId = Guid.NewGuid();
        var now = DateTime.UtcNow;
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
        return scheduledPostId;
    }

    [Fact]
    public async Task Handle_ForADayWithMultipleSnapshots_UsesWeightedEngagementRate_NotAverageOfPerSnapshotRates()
    {
        var (dbContext, brandProfileId, socialAccountId, contentItemId) = await SeedBrandAsync();
        var today = DateTime.UtcNow.Date.AddHours(10);

        // Snapshot A: 10 views, 10 engagements -> rate 1.0. Snapshot B: 10,000 views, 100 engagements
        // -> rate 0.01. Naive average of rates: 0.505. Weighted: 110/10,010 ~= 0.011.
        var postA = AddScheduledPost(dbContext, brandProfileId, socialAccountId, contentItemId);
        dbContext.PostAnalytics.Add(new PostAnalytics
        {
            Id = Guid.NewGuid(), ScheduledPostId = postA, Platform = SocialPlatform.Facebook,
            RecordedAt = today, Views = 10, Likes = 10, EngagementRate = 1.0m
        });

        var postB = AddScheduledPost(dbContext, brandProfileId, socialAccountId, contentItemId);
        dbContext.PostAnalytics.Add(new PostAnalytics
        {
            Id = Guid.NewGuid(), ScheduledPostId = postB, Platform = SocialPlatform.Facebook,
            RecordedAt = today, Views = 10_000, Likes = 100, EngagementRate = 0.01m
        });

        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetDashboardChartsQueryHandler(dbContext);
        var result = await handler.Handle(new GetDashboardChartsQuery(brandProfileId, null, Days: 1), CancellationToken.None);

        Assert.True(result.Succeeded, result.ErrorMessage);
        var point = Assert.Single(result.Data!.Points);
        Assert.Equal(Math.Round(110m / 10_010m, 4), point.EngagementRate);
        Assert.NotEqual(0.505m, point.EngagementRate);
    }

    [Fact]
    public async Task Handle_ForADayWithNoViews_EngagementRateIsNullNotZero()
    {
        var (dbContext, brandProfileId, socialAccountId, contentItemId) = await SeedBrandAsync();
        var today = DateTime.UtcNow.Date.AddHours(10);

        var post = AddScheduledPost(dbContext, brandProfileId, socialAccountId, contentItemId);
        dbContext.PostAnalytics.Add(new PostAnalytics
        {
            Id = Guid.NewGuid(), ScheduledPostId = post, Platform = SocialPlatform.Facebook,
            RecordedAt = today, Views = null, Likes = 5, EngagementRate = null
        });

        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetDashboardChartsQueryHandler(dbContext);
        var result = await handler.Handle(new GetDashboardChartsQuery(brandProfileId, null, Days: 1), CancellationToken.None);

        Assert.True(result.Succeeded, result.ErrorMessage);
        var point = Assert.Single(result.Data!.Points);
        Assert.Null(point.EngagementRate);
    }

    [Fact]
    public async Task Handle_SumsViewsAndUniqueViewersPerDay_UnderTheCorrectNames()
    {
        var (dbContext, brandProfileId, socialAccountId, contentItemId) = await SeedBrandAsync();
        var today = DateTime.UtcNow.Date.AddHours(10);

        var post = AddScheduledPost(dbContext, brandProfileId, socialAccountId, contentItemId);
        dbContext.PostAnalytics.Add(new PostAnalytics
        {
            Id = Guid.NewGuid(), ScheduledPostId = post, Platform = SocialPlatform.Facebook,
            RecordedAt = today, Views = 300, UniqueViewers = 250, Likes = 1
        });

        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetDashboardChartsQueryHandler(dbContext);
        var result = await handler.Handle(new GetDashboardChartsQuery(brandProfileId, null, Days: 1), CancellationToken.None);

        Assert.True(result.Succeeded, result.ErrorMessage);
        var point = Assert.Single(result.Data!.Points);
        Assert.Equal(300, point.Views);
        Assert.Equal(250, point.UniqueViewers);
    }

    [Fact]
    public async Task Handle_BucketsSnapshotsByTheirOwnRecordedDay_NotToday()
    {
        var (dbContext, brandProfileId, socialAccountId, contentItemId) = await SeedBrandAsync();
        var twoDaysAgo = DateTime.UtcNow.Date.AddDays(-2).AddHours(9);

        var post = AddScheduledPost(dbContext, brandProfileId, socialAccountId, contentItemId);
        dbContext.PostAnalytics.Add(new PostAnalytics
        {
            Id = Guid.NewGuid(), ScheduledPostId = post, Platform = SocialPlatform.Facebook,
            RecordedAt = twoDaysAgo, Views = 50, Likes = 5
        });

        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetDashboardChartsQueryHandler(dbContext);
        var result = await handler.Handle(new GetDashboardChartsQuery(brandProfileId, null, Days: 7), CancellationToken.None);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Equal(7, result.Data!.Points.Count);
        var bucket = result.Data!.Points.Single(p => p.Date == DateOnly.FromDateTime(twoDaysAgo));
        Assert.Equal(50, bucket.Views);
        var emptyDay = result.Data!.Points.Single(p => p.Date == DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.Equal(0, emptyDay.Views);
    }
}
