using NSubstitute;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Analytics.Common;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.SocialMedia;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Persistence;
using Xunit;

namespace Rawaj.Application.Tests.Features.Analytics.Common;

/// <summary>
/// Unit tests for the shared sync helper behind both the manual "sync now" endpoint and
/// AnalyticsSyncHostedService. Per the implementation plan's own convention (no other hosted service
/// in this codebase has a direct test, AI pipeline included), the hosted service itself isn't tested
/// here - these tests cover the actual sync logic it delegates to.
/// </summary>
public class PostAnalyticsSyncerTests
{
    private static async Task<(AppDbContext DbContext, ScheduledPost Post)> SeedPublishedPostAsync(
        string databaseName, SocialPlatform platform = SocialPlatform.Facebook, string? postId = "meta-post-1",
        ScheduledPostStatus status = ScheduledPostStatus.Published)
    {
        var dbContext = TestDbContextFactory.Create(databaseName);
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
            Platform = platform,
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
            Platform = platform,
            Language = Language.En,
            Content = "Post body",
            Status = ContentStatus.Approved,
            CreatedAt = now,
            UpdatedAt = now,
            RowVersion = new byte[8]
        });

        var post = new ScheduledPost
        {
            Id = scheduledPostId,
            ContentItemId = contentItemId,
            SocialAccountId = socialAccountId,
            BrandProfileId = brandProfileId,
            ScheduledAt = now,
            Status = status,
            PostId = postId,
            PublishedAt = status == ScheduledPostStatus.Published ? now : null,
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.ScheduledPosts.Add(post);

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return (dbContext, post);
    }

    private static ITokenEncryptor FakeTokenEncryptor()
    {
        var tokenEncryptor = Substitute.For<ITokenEncryptor>();
        tokenEncryptor.Decrypt(Arg.Any<string>()).Returns("decrypted-token");
        return tokenEncryptor;
    }

    private static ISocialAnalyticsProvider FakeProvider(PostMetricsResult result, SocialPlatform platform = SocialPlatform.Facebook)
    {
        var provider = Substitute.For<ISocialAnalyticsProvider>();
        provider.Platform.Returns(platform);
        provider.GetMetricsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(result);
        return provider;
    }

    [Fact]
    public async Task SyncOneAsync_WhenBothCallsSucceed_WritesViewsAndUniqueViewers()
    {
        var (dbContext, post) = await SeedPublishedPostAsync(nameof(SyncOneAsync_WhenBothCallsSucceed_WritesViewsAndUniqueViewers));
        var provider = FakeProvider(PostMetricsResult.Success(
            views: 500, uniqueViewers: 420, likes: 10, comments: 3, shares: 2, insightsAvailable: true));

        var outcome = await PostAnalyticsSyncer.SyncOneAsync(dbContext, FakeTokenEncryptor(), [provider], post, CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        Assert.True(outcome.Succeeded, outcome.ErrorMessage);
        Assert.Equal(500, outcome.Analytics!.Views);
        Assert.Equal(420, outcome.Analytics!.UniqueViewers);
        Assert.Single(dbContext.PostAnalytics);
    }

    [Fact]
    public async Task SyncOneAsync_WhenInsightsUnavailableButEngagementSucceeds_StillWritesASnapshot()
    {
        // Partial-availability handling: PostMetricsResult.Succeeded stays true when only the
        // insights call failed (Phase 1 behavior) - the syncer must still record engagement data
        // rather than treating it as a total failure.
        var (dbContext, post) = await SeedPublishedPostAsync(nameof(SyncOneAsync_WhenInsightsUnavailableButEngagementSucceeds_StillWritesASnapshot));
        var provider = FakeProvider(PostMetricsResult.Success(
            views: null, uniqueViewers: null, likes: 10, comments: 3, shares: 2,
            insightsAvailable: false, errorMessage: "read_insights not granted"));

        var outcome = await PostAnalyticsSyncer.SyncOneAsync(dbContext, FakeTokenEncryptor(), [provider], post, CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        Assert.True(outcome.Succeeded, outcome.ErrorMessage);
        Assert.Null(outcome.Analytics!.Views);
        Assert.Null(outcome.Analytics!.UniqueViewers);
        Assert.Equal(10, outcome.Analytics!.Likes);
        Assert.Single(dbContext.PostAnalytics);
    }

    [Fact]
    public async Task SyncOneAsync_WhenProviderReportsTotalFailure_WritesNoSnapshotRow()
    {
        var (dbContext, post) = await SeedPublishedPostAsync(nameof(SyncOneAsync_WhenProviderReportsTotalFailure_WritesNoSnapshotRow));
        var provider = FakeProvider(PostMetricsResult.Failure("both calls failed"));

        var outcome = await PostAnalyticsSyncer.SyncOneAsync(dbContext, FakeTokenEncryptor(), [provider], post, CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        Assert.False(outcome.Succeeded);
        Assert.Null(outcome.Analytics);
        Assert.Empty(dbContext.PostAnalytics);
    }

    [Fact]
    public async Task SyncOneAsync_WhenPostIsNotPublished_FailsWithoutCallingProvider()
    {
        var (dbContext, post) = await SeedPublishedPostAsync(
            nameof(SyncOneAsync_WhenPostIsNotPublished_FailsWithoutCallingProvider), status: ScheduledPostStatus.Pending);
        var provider = FakeProvider(PostMetricsResult.Success(
            views: 1, uniqueViewers: 1, likes: 1, comments: 1, shares: 1, insightsAvailable: true));

        var outcome = await PostAnalyticsSyncer.SyncOneAsync(dbContext, FakeTokenEncryptor(), [provider], post, CancellationToken.None);

        Assert.False(outcome.Succeeded);
        await provider.DidNotReceive().GetMetricsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.Empty(dbContext.PostAnalytics);
    }

    [Fact]
    public async Task SyncOneAsync_WhenNoProviderSupportsThePlatform_Fails()
    {
        var (dbContext, post) = await SeedPublishedPostAsync(
            nameof(SyncOneAsync_WhenNoProviderSupportsThePlatform_Fails), platform: SocialPlatform.Instagram);
        var facebookOnlyProvider = FakeProvider(
            PostMetricsResult.Success(views: 1, uniqueViewers: 1, likes: 1, comments: 1, shares: 1, insightsAvailable: true),
            SocialPlatform.Facebook);

        var outcome = await PostAnalyticsSyncer.SyncOneAsync(
            dbContext, FakeTokenEncryptor(), [facebookOnlyProvider], post, CancellationToken.None);

        Assert.False(outcome.Succeeded);
        Assert.Contains("not yet supported", outcome.ErrorMessage);
        Assert.Empty(dbContext.PostAnalytics);
    }

    [Fact]
    public async Task SyncOneAsync_CalledTwiceForSamePost_AppendsTwoSnapshotsWithoutOverwriting()
    {
        // Snapshots are append-only history, not current state - two sync passes over the same
        // post (e.g. two overlapping worker ticks) must both survive as distinct rows.
        var (dbContext, post) = await SeedPublishedPostAsync(nameof(SyncOneAsync_CalledTwiceForSamePost_AppendsTwoSnapshotsWithoutOverwriting));
        var provider = FakeProvider(PostMetricsResult.Success(
            views: 100, uniqueViewers: 90, likes: 5, comments: 1, shares: 0, insightsAvailable: true));

        var first = await PostAnalyticsSyncer.SyncOneAsync(dbContext, FakeTokenEncryptor(), [provider], post, CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var second = await PostAnalyticsSyncer.SyncOneAsync(dbContext, FakeTokenEncryptor(), [provider], post, CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.NotEqual(first.Analytics!.Id, second.Analytics!.Id);
        Assert.Equal(2, dbContext.PostAnalytics.Count());
    }

    [Fact]
    public async Task SyncOneAsync_OneFailingPostDoesNotAffectAnother_WhenRunConcurrentlyAcrossScopes()
    {
        // Isolation prerequisite for the hosted service's bounded-concurrency tick: each post is
        // synced through its own DbContext (its own "scope"), so a failure or exception on one must
        // never corrupt or block another running at the same time. The concurrency cap itself
        // (SemaphoreSlim in AnalyticsSyncHostedService) isn't unit-tested here, consistent with this
        // codebase's convention of not testing hosted services directly.
        const string databaseName = nameof(SyncOneAsync_OneFailingPostDoesNotAffectAnother_WhenRunConcurrentlyAcrossScopes);
        var (seedContext, failingPost) = await SeedPublishedPostAsync(databaseName, postId: "meta-post-failing");
        var (_, succeedingPost) = await SeedPublishedPostAsync(databaseName, postId: "meta-post-succeeding");
        seedContext.Dispose();

        var failingProvider = FakeProvider(PostMetricsResult.Failure("boom"));
        var succeedingProvider = FakeProvider(PostMetricsResult.Success(
            views: 10, uniqueViewers: 8, likes: 1, comments: 0, shares: 0, insightsAvailable: true));

        // Simulates two per-post scopes (each with its own DbContext, per AnalyticsSyncHostedService)
        // racing concurrently against the same underlying InMemory database.
        var failingTask = Task.Run(async () =>
        {
            using var scopedContext = TestDbContextFactory.Create(databaseName);
            var outcome = await PostAnalyticsSyncer.SyncOneAsync(
                scopedContext, FakeTokenEncryptor(), [failingProvider], failingPost, CancellationToken.None);
            await scopedContext.SaveChangesAsync(CancellationToken.None);
            return outcome;
        });

        var succeedingTask = Task.Run(async () =>
        {
            using var scopedContext = TestDbContextFactory.Create(databaseName);
            var outcome = await PostAnalyticsSyncer.SyncOneAsync(
                scopedContext, FakeTokenEncryptor(), [succeedingProvider], succeedingPost, CancellationToken.None);
            await scopedContext.SaveChangesAsync(CancellationToken.None);
            return outcome;
        });

        var results = await Task.WhenAll(failingTask, succeedingTask);

        Assert.False(results[0].Succeeded);
        Assert.True(results[1].Succeeded, results[1].ErrorMessage);

        using var verifyContext = TestDbContextFactory.Create(databaseName);
        Assert.Single(verifyContext.PostAnalytics);
        Assert.Equal(succeedingPost.Id, verifyContext.PostAnalytics.Single().ScheduledPostId);
    }

    [Fact]
    public async Task GetDuePostIdsAsync_IncludesPublishedPostWithNoPriorSnapshot()
    {
        var (dbContext, post) = await SeedPublishedPostAsync(nameof(GetDuePostIdsAsync_IncludesPublishedPostWithNoPriorSnapshot));

        var dueIds = await PostAnalyticsSyncer.GetDuePostIdsAsync(dbContext, DateTime.UtcNow.AddHours(-6), CancellationToken.None);

        Assert.Contains(post.Id, dueIds);
    }

    [Fact]
    public async Task GetDuePostIdsAsync_ExcludesPostSnapshottedWithinTheWindow()
    {
        var (dbContext, post) = await SeedPublishedPostAsync(nameof(GetDuePostIdsAsync_ExcludesPostSnapshottedWithinTheWindow));
        dbContext.PostAnalytics.Add(new PostAnalytics
        {
            Id = Guid.NewGuid(),
            ScheduledPostId = post.Id,
            Platform = SocialPlatform.Facebook,
            RecordedAt = DateTime.UtcNow.AddHours(-1)
        });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var dueIds = await PostAnalyticsSyncer.GetDuePostIdsAsync(dbContext, DateTime.UtcNow.AddHours(-6), CancellationToken.None);

        Assert.DoesNotContain(post.Id, dueIds);
    }

    [Fact]
    public async Task GetDuePostIdsAsync_IncludesPostWhoseOnlySnapshotIsOlderThanTheWindow()
    {
        // A post is due again once its most recent snapshot falls outside the sync window - this is
        // what lets a failed/old sync retry on a later tick without any counted retry field.
        var (dbContext, post) = await SeedPublishedPostAsync(nameof(GetDuePostIdsAsync_IncludesPostWhoseOnlySnapshotIsOlderThanTheWindow));
        dbContext.PostAnalytics.Add(new PostAnalytics
        {
            Id = Guid.NewGuid(),
            ScheduledPostId = post.Id,
            Platform = SocialPlatform.Facebook,
            RecordedAt = DateTime.UtcNow.AddHours(-12)
        });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var dueIds = await PostAnalyticsSyncer.GetDuePostIdsAsync(dbContext, DateTime.UtcNow.AddHours(-6), CancellationToken.None);

        Assert.Contains(post.Id, dueIds);
    }

    [Fact]
    public async Task GetDuePostIdsAsync_ExcludesPostsThatArentPublishedOrHaveNoPostId()
    {
        var (dbContext, pendingPost) = await SeedPublishedPostAsync(
            nameof(GetDuePostIdsAsync_ExcludesPostsThatArentPublishedOrHaveNoPostId), status: ScheduledPostStatus.Pending);
        var (dbContext2, noPostIdPost) = await SeedPublishedPostAsync(
            nameof(GetDuePostIdsAsync_ExcludesPostsThatArentPublishedOrHaveNoPostId), postId: null);
        dbContext2.Dispose();

        var dueIds = await PostAnalyticsSyncer.GetDuePostIdsAsync(dbContext, DateTime.UtcNow.AddHours(-6), CancellationToken.None);

        Assert.DoesNotContain(pendingPost.Id, dueIds);
        Assert.DoesNotContain(noPostIdPost.Id, dueIds);
    }

    [Fact]
    public async Task GetDuePostIdsAsync_SpansMultipleSocialAccountsInOnePass()
    {
        // No special-casing needed per Social Account/Page - the query naturally covers every
        // tenant/brand/account in a single pass, same as every other hosted service in this codebase.
        const string databaseName = nameof(GetDuePostIdsAsync_SpansMultipleSocialAccountsInOnePass);
        var (dbContext, facebookPost) = await SeedPublishedPostAsync(databaseName, platform: SocialPlatform.Facebook, postId: "fb-post");
        var (_, instagramPost) = await SeedPublishedPostAsync(databaseName, platform: SocialPlatform.Instagram, postId: "ig-post");

        var dueIds = await PostAnalyticsSyncer.GetDuePostIdsAsync(dbContext, DateTime.UtcNow.AddHours(-6), CancellationToken.None);

        Assert.Contains(facebookPost.Id, dueIds);
        Assert.Contains(instagramPost.Id, dueIds);
    }
}
