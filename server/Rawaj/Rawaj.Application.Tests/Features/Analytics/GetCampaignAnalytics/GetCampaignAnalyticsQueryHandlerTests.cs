using NSubstitute;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Features.Analytics.GetCampaignAnalytics;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.SocialMedia;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Persistence;
using Xunit;

namespace Rawaj.Application.Tests.Features.Analytics.GetCampaignAnalytics;

/// <summary>
/// End-to-end (seeded InMemory DB) coverage for the Phase 4 KPI fix: the campaign-level rollup must
/// be a weighted SUM(Engagements)/SUM(Views) and must expose summed unique-viewer events under the
/// UniqueViewers name, never labeled/implying deduplicated "Reach".
/// </summary>
public class GetCampaignAnalyticsQueryHandlerTests
{
    private static async Task<(AppDbContext DbContext, Guid TenantId, Guid CampaignId)> SeedCampaignWithPostsAsync(
        params (long? Views, long? UniqueViewers, int? Likes, int? Comments, int? Shares)[] posts)
    {
        var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var brandProfileId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
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

        dbContext.MarketingCampaigns.Add(new MarketingCampaign
        {
            Id = campaignId,
            BrandProfileId = brandProfileId,
            CreatedBy = Guid.NewGuid(),
            Name = "Test Campaign",
            Status = CampaignStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            RowVersion = new byte[8]
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
                CampaignId = campaignId,
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
                CampaignId = campaignId,
                ScheduledAt = now,
                Status = ScheduledPostStatus.Published,
                PostId = $"meta-post-{scheduledPostId:N}",
                PublishedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            });

            var engagementRate = post.Views is > 0
                ? Math.Round((decimal)((post.Likes ?? 0) + (post.Comments ?? 0) + (post.Shares ?? 0)) / post.Views.Value, 4)
                : (decimal?)null;

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
                Shares = post.Shares,
                EngagementRate = engagementRate
            });
        }

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return (dbContext, tenantId, campaignId);
    }

    private static GetCampaignAnalyticsQueryHandler BuildHandler(AppDbContext dbContext, Guid tenantId)
    {
        var currentTenantContext = Substitute.For<ICurrentTenantContext>();
        currentTenantContext.TenantId.Returns(tenantId);
        return new GetCampaignAnalyticsQueryHandler(dbContext, currentTenantContext);
    }

    [Fact]
    public async Task Handle_WithMultiplePostsOfDifferentRates_UsesWeightedEngagementRate_NotNaiveAverage()
    {
        // Post A: 10 views, 10 engagements -> 1.0 rate. Post B: 10,000 views, 100 engagements -> 0.01
        // rate. Naive average would be 0.505; weighted is (10+100)/(10+10000) ~= 0.011.
        var (dbContext, tenantId, campaignId) = await SeedCampaignWithPostsAsync(
            (Views: 10, UniqueViewers: 8, Likes: 10, Comments: 0, Shares: 0),
            (Views: 10_000, UniqueViewers: 9_000, Likes: 100, Comments: 0, Shares: 0));
        var handler = BuildHandler(dbContext, tenantId);

        var result = await handler.Handle(new GetCampaignAnalyticsQuery(campaignId), CancellationToken.None);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Equal(Math.Round(110m / 10_010m, 4), result.Data!.AverageEngagementRate);
        Assert.NotEqual(0.505m, result.Data!.AverageEngagementRate);
    }

    [Fact]
    public async Task Handle_SumsUniqueViewersAcrossPosts_ExposedAsUniqueViewers_NotReach()
    {
        var (dbContext, tenantId, campaignId) = await SeedCampaignWithPostsAsync(
            (Views: 100, UniqueViewers: 80, Likes: 5, Comments: 1, Shares: 0),
            (Views: 200, UniqueViewers: 150, Likes: 3, Comments: 0, Shares: 1));
        var handler = BuildHandler(dbContext, tenantId);

        var result = await handler.Handle(new GetCampaignAnalyticsQuery(campaignId), CancellationToken.None);

        Assert.True(result.Succeeded, result.ErrorMessage);
        // A gross sum of per-post unique-viewer counts is not deduplicated campaign-level reach -
        // the field is named/typed as TotalUniqueViewers, never TotalReach, precisely so it can't be
        // read as such.
        Assert.Equal(230, result.Data!.TotalUniqueViewers);
        Assert.Equal(300, result.Data!.TotalViews);
    }

    [Fact]
    public async Task Handle_WhenNoPostHasViews_EngagementRateIsNullAndUnavailable_NotZero()
    {
        var (dbContext, tenantId, campaignId) = await SeedCampaignWithPostsAsync(
            (Views: null, UniqueViewers: null, Likes: 5, Comments: 0, Shares: 0));
        var handler = BuildHandler(dbContext, tenantId);

        var result = await handler.Handle(new GetCampaignAnalyticsQuery(campaignId), CancellationToken.None);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Null(result.Data!.AverageEngagementRate);
        Assert.False(result.Data!.EngagementRateAvailable);
        Assert.False(result.Data!.ViewsAvailable);
    }

    [Fact]
    public async Task Handle_SinglePost_CampaignRateEqualsThatPostsOwnEngagementRate()
    {
        var (dbContext, tenantId, campaignId) = await SeedCampaignWithPostsAsync(
            (Views: 200, UniqueViewers: 150, Likes: 10, Comments: 5, Shares: 5));
        var handler = BuildHandler(dbContext, tenantId);

        var result = await handler.Handle(new GetCampaignAnalyticsQuery(campaignId), CancellationToken.None);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Equal(0.1m, result.Data!.AverageEngagementRate);
    }
}
