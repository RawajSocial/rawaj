using NSubstitute;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Features.Analytics.GetBrandAnalytics;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.SocialMedia;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Persistence;
using Xunit;

namespace Rawaj.Application.Tests.Features.Analytics.GetBrandAnalytics;

/// <summary>
/// Phase 6: BrandAnalyticsOverview gains a Bottom Posts surface and the *Available flag pattern
/// already present on CampaignAnalyticsSummary, per analytics-spec.md §7 / the implementation plan's
/// explicit Phase 6 scope.
/// </summary>
public class GetBrandAnalyticsQueryHandlerTests
{
    private static async Task<(AppDbContext DbContext, Guid TenantId, Guid BrandProfileId)> SeedBrandWithPostsAsync(
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

        return (dbContext, tenantId, brandProfileId);
    }

    private static GetBrandAnalyticsQueryHandler BuildHandler(AppDbContext dbContext, Guid tenantId)
    {
        var currentTenantContext = Substitute.For<ICurrentTenantContext>();
        currentTenantContext.TenantId.Returns(tenantId);
        return new GetBrandAnalyticsQueryHandler(dbContext, currentTenantContext);
    }

    [Fact]
    public async Task Handle_BottomPosts_RanksAscendingByUniqueViewers()
    {
        var (dbContext, tenantId, brandProfileId) = await SeedBrandWithPostsAsync(
            (Views: 10, UniqueViewers: 5, Likes: 1, Comments: 0, Shares: 0),
            (Views: 300, UniqueViewers: 250, Likes: 10, Comments: 2, Shares: 1),
            (Views: 100, UniqueViewers: 90, Likes: 3, Comments: 1, Shares: 0));
        var handler = BuildHandler(dbContext, tenantId);

        var result = await handler.Handle(new GetBrandAnalyticsQuery(brandProfileId), CancellationToken.None);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Equal(5, result.Data!.BottomPosts[0].UniqueViewers);
        Assert.Equal(10, result.Data!.BottomPosts[0].Views);
        Assert.Equal(250, result.Data!.BottomPosts[^1].UniqueViewers);
        Assert.Equal(250, result.Data!.TopPosts[0].UniqueViewers);
    }

    [Fact]
    public async Task Handle_AvailabilityFlags_TrueWhenAnyPostHasData_FalseWhenNoneDo()
    {
        var (dbContext, tenantId, brandProfileId) = await SeedBrandWithPostsAsync(
            (Views: 100, UniqueViewers: 80, Likes: 5, Comments: 1, Shares: 0));
        var handler = BuildHandler(dbContext, tenantId);

        var result = await handler.Handle(new GetBrandAnalyticsQuery(brandProfileId), CancellationToken.None);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.True(result.Data!.ViewsAvailable);
        Assert.True(result.Data!.UniqueViewersAvailable);
        Assert.True(result.Data!.EngagementRateAvailable);
    }

    [Fact]
    public async Task Handle_AvailabilityFlags_AllFalse_WhenNoPostHasViews()
    {
        var (dbContext, tenantId, brandProfileId) = await SeedBrandWithPostsAsync(
            (Views: null, UniqueViewers: null, Likes: 5, Comments: 1, Shares: 0));
        var handler = BuildHandler(dbContext, tenantId);

        var result = await handler.Handle(new GetBrandAnalyticsQuery(brandProfileId), CancellationToken.None);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.False(result.Data!.ViewsAvailable);
        Assert.False(result.Data!.UniqueViewersAvailable);
        Assert.False(result.Data!.EngagementRateAvailable);
    }
}
