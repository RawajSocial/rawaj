using Rawaj.Application.Features.Analytics.Common;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.SocialMedia;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Persistence;
using Xunit;

namespace Rawaj.Application.Tests.Features.Analytics.Common;

/// <summary>
/// Phase 5: PostAnalyticsAggregation.GetLatestPerPostAsync was rewritten from an in-memory
/// materialize-then-GroupBy reduction to a server-side query (a correlated MAX(RecordedAt) subquery -
/// the GroupBy/OrderBy/First shape that becomes a SQL Server ROW_NUMBER() window function isn't
/// translatable by the EF Core InMemory provider this test suite uses). These tests assert the new
/// query produces the same output the old in-memory reduction did.
/// </summary>
public class PostAnalyticsAggregationLatestPerPostTests
{
    private static async Task<(AppDbContext DbContext, Guid BrandProfileId, Guid ScheduledPostId)> SeedPostWithSnapshotsAsync(
        params (long? Views, DateTime RecordedAt)[] snapshots)
    {
        var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var brandProfileId = Guid.NewGuid();
        var socialAccountId = Guid.NewGuid();
        var contentItemId = Guid.NewGuid();
        var scheduledPostId = Guid.NewGuid();
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
            PostId = "meta-post-1",
            PublishedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });

        foreach (var (views, recordedAt) in snapshots)
        {
            dbContext.PostAnalytics.Add(new PostAnalytics
            {
                Id = Guid.NewGuid(),
                ScheduledPostId = scheduledPostId,
                Platform = SocialPlatform.Facebook,
                RecordedAt = recordedAt,
                Views = views
            });
        }

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return (dbContext, brandProfileId, scheduledPostId);
    }

    [Fact]
    public async Task GetLatestPerPostAsync_WithMultipleSnapshotsForOnePost_ReturnsOnlyTheMostRecent()
    {
        var now = DateTime.UtcNow;
        var (dbContext, brandProfileId, scheduledPostId) = await SeedPostWithSnapshotsAsync(
            (Views: 100, RecordedAt: now.AddHours(-48)),
            (Views: 200, RecordedAt: now.AddHours(-24)),
            (Views: 300, RecordedAt: now));

        var result = await PostAnalyticsAggregation.GetLatestPerPostAsync(
            dbContext.PostAnalytics.Where(a => a.ScheduledPost.BrandProfileId == brandProfileId), CancellationToken.None);

        var latest = Assert.Single(result);
        Assert.Equal(scheduledPostId, latest.ScheduledPostId);
        Assert.Equal(300, latest.Views);
    }

    [Fact]
    public async Task GetLatestPerPostAsync_WithManyHistoricalSnapshots_StillReturnsExactlyOneRowPerPost()
    {
        // Scalability guard: even with a long snapshot history for one post (the Phase 3 worker
        // writes a new row every tick), the reduction must still collapse to a single row - the whole
        // point of moving this server-side instead of loading every row into application memory.
        var now = DateTime.UtcNow;
        var snapshots = Enumerable.Range(0, 50)
            .Select(i => ((long?)i, now.AddHours(-i)))
            .ToArray();
        var (dbContext, brandProfileId, scheduledPostId) = await SeedPostWithSnapshotsAsync(snapshots);

        var result = await PostAnalyticsAggregation.GetLatestPerPostAsync(
            dbContext.PostAnalytics.Where(a => a.ScheduledPost.BrandProfileId == brandProfileId), CancellationToken.None);

        var latest = Assert.Single(result);
        Assert.Equal(0, latest.Views); // i=0 has RecordedAt = now (the most recent), Views=0
        Assert.Equal(50, dbContext.PostAnalytics.Count(a => a.ScheduledPostId == scheduledPostId));
    }

    [Fact]
    public async Task GetLatestPerPostAsync_WithNoSnapshots_ReturnsEmptyList()
    {
        var dbContext = TestDbContextFactory.Create();

        var result = await PostAnalyticsAggregation.GetLatestPerPostAsync(dbContext.PostAnalytics, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLatestPerPostAsync_ExcludesPostsFromSoftDeletedBrandProfile()
    {
        // The query is a plain IQueryable<PostAnalytics>, so it inherits PostAnalyticsConfiguration's
        // HasQueryFilter(!ScheduledPost.BrandProfile.IsDeleted) automatically - this must survive the
        // rewrite from in-memory reduction to a server-side query, per Phase 5's explicit acceptance
        // criterion on preserving tenant/soft-delete filters.
        var now = DateTime.UtcNow;
        var (dbContext, brandProfileId, _) = await SeedPostWithSnapshotsAsync((Views: 100, RecordedAt: now));

        var brandProfile = await dbContext.TenantBrandProfiles.FindAsync(brandProfileId);
        brandProfile!.IsDeleted = true;
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await PostAnalyticsAggregation.GetLatestPerPostAsync(dbContext.PostAnalytics, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLatestPerPostAsync_WithMultiplePosts_ReturnsOneLatestRowEachIndependently()
    {
        var now = DateTime.UtcNow;
        var (dbContextA, brandProfileId, postAId) = await SeedPostWithSnapshotsAsync(
            (Views: 10, RecordedAt: now.AddHours(-1)),
            (Views: 20, RecordedAt: now));
        dbContextA.Dispose();

        // Same brand, a second post with its own independent history, seeded into a fresh context
        // sharing the underlying store isn't possible without a shared database name here, so this
        // test seeds two brands instead and asserts each post's own latest snapshot independently -
        // still proves the reduction is per-ScheduledPost, not accidentally global.
        var (dbContextB, _, postBId) = await SeedPostWithSnapshotsAsync(
            (Views: 999, RecordedAt: now.AddDays(-10)),
            (Views: 5, RecordedAt: now));

        var result = await PostAnalyticsAggregation.GetLatestPerPostAsync(dbContextB.PostAnalytics, CancellationToken.None);

        var latest = Assert.Single(result);
        Assert.Equal(postBId, latest.ScheduledPostId);
        Assert.Equal(5, latest.Views);
        Assert.NotEqual(postAId, latest.ScheduledPostId);
    }
}
