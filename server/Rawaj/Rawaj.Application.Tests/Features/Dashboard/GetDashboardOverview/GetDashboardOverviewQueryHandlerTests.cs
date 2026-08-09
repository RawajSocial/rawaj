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
}
