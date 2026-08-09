using NSubstitute;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Analytics.SyncPostAnalytics;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.SocialMedia;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Persistence;
using Xunit;

namespace Rawaj.Application.Tests.Features.Analytics;

public class SyncPostAnalyticsCommandHandlerTests
{
    private static async Task<(AppDbContext DbContext, Guid TenantId, Guid ScheduledPostId)> SeedAsync()
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

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return (dbContext, tenantId, scheduledPostId);
    }

    private static SyncPostAnalyticsCommandHandler BuildHandler(
        AppDbContext dbContext, Guid tenantId, ISocialAnalyticsProvider provider)
    {
        var tokenEncryptor = Substitute.For<ITokenEncryptor>();
        tokenEncryptor.Decrypt(Arg.Any<string>()).Returns("decrypted-token");

        var currentTenantContext = Substitute.For<ICurrentTenantContext>();
        currentTenantContext.TenantId.Returns(tenantId);

        return new SyncPostAnalyticsCommandHandler(dbContext, tokenEncryptor, currentTenantContext, [provider]);
    }

    private static ISocialAnalyticsProvider FakeProvider(PostMetricsResult result)
    {
        var provider = Substitute.For<ISocialAnalyticsProvider>();
        provider.Platform.Returns(SocialPlatform.Facebook);
        provider.GetMetricsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(result);
        return provider;
    }

    [Fact]
    public async Task Handle_WhenBothCallsSucceed_WritesViewsAndUniqueViewersOntoLegacyColumns()
    {
        // PostAnalytics/PostAnalyticsSnapshot keep the old Impressions/Reach property names until
        // Phase 2 ships the rename - this deliberately temporary mapping is what's under test here.
        var (dbContext, tenantId, scheduledPostId) = await SeedAsync();
        var provider = FakeProvider(PostMetricsResult.Success(
            views: 500, uniqueViewers: 420, likes: 10, comments: 3, shares: 2, insightsAvailable: true));
        var handler = BuildHandler(dbContext, tenantId, provider);

        var result = await handler.Handle(new SyncPostAnalyticsCommand(scheduledPostId), CancellationToken.None);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Equal(500, result.Data!.Impressions);
        Assert.Equal(420, result.Data!.Reach);
        Assert.Equal(10, result.Data!.Likes);
        Assert.Single(dbContext.PostAnalytics);
        var stored = dbContext.PostAnalytics.Single();
        Assert.Equal(500, stored.Impressions);
        Assert.Equal(420, stored.Reach);
    }

    [Fact]
    public async Task Handle_WhenInsightsUnavailableButEngagementSucceeds_StillWritesASnapshot()
    {
        var (dbContext, tenantId, scheduledPostId) = await SeedAsync();
        var provider = FakeProvider(PostMetricsResult.Success(
            views: null, uniqueViewers: null, likes: 10, comments: 3, shares: 2,
            insightsAvailable: false, errorMessage: "read_insights not granted"));
        var handler = BuildHandler(dbContext, tenantId, provider);

        var result = await handler.Handle(new SyncPostAnalyticsCommand(scheduledPostId), CancellationToken.None);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Null(result.Data!.Impressions);
        Assert.Null(result.Data!.Reach);
        Assert.Equal(10, result.Data!.Likes);
        Assert.Single(dbContext.PostAnalytics);
    }

    [Fact]
    public async Task Handle_WhenProviderReportsTotalFailure_WritesNoSnapshotRow()
    {
        var (dbContext, tenantId, scheduledPostId) = await SeedAsync();
        var provider = FakeProvider(PostMetricsResult.Failure("both calls failed"));
        var handler = BuildHandler(dbContext, tenantId, provider);

        var result = await handler.Handle(new SyncPostAnalyticsCommand(scheduledPostId), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Empty(dbContext.PostAnalytics);
    }
}
