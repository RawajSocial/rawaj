using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Campaigns.DeleteCampaign;
using Rawaj.Application.Features.Campaigns.GetCampaignDeleteSummary;
using Rawaj.Application.Features.Campaigns.GetCampaigns;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.SocialMedia;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Persistence;
using Xunit;

namespace Rawaj.Application.Tests.Features.Campaigns;

/// <summary>
/// Covers the campaign delete feature end to end at the handler level: cascading soft-delete to
/// content/images/scheduled posts, leaving already-published posts live on their platform, deferred
/// native cancellation of pending ones, and the delete-preview counts the confirmation modal reads
/// before the user commits.
///
/// <para>The delete is deliberately split in two — the command does database work only, and the
/// slow external work (platform cancels, Cloudinary purges) runs off an outbox message so the user
/// isn't held on a spinner. Tests that care about the external half call <c>RunCleanupAsync</c>,
/// which drives the real queued payload exactly as the hosted service would.</para>
/// </summary>
public class CampaignDeleteTests
{
    private static async Task<(AppDbContext DbContext, Guid TenantId, Guid BrandProfileId, Guid CampaignId)> SeedAsync()
    {
        var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var brandProfileId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
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
            Name = "Summer Sale",
            Status = CampaignStatus.Active,
            TargetPlatforms = [],
            CreatedAt = now,
            UpdatedAt = now
        });

        await dbContext.SaveChangesAsync(CancellationToken.None);
        return (dbContext, tenantId, brandProfileId, campaignId);
    }

    private static ContentItem AddContentItem(AppDbContext dbContext, Guid campaignId, Guid brandProfileId, Guid tenantId)
    {
        var item = new ContentItem
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            TenantId = tenantId,
            BrandProfileId = brandProfileId,
            GenerationMode = GenerationMode.Campaign,
            CreatedBy = Guid.NewGuid(),
            ContentType = ContentType.Post,
            Platform = SocialPlatform.Facebook,
            Language = Language.Ar,
            Content = "منشور تجريبي",
            Status = ContentStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.ContentItems.Add(item);
        return item;
    }

    private static VisualAsset AddVisualAsset(AppDbContext dbContext, Guid campaignId, Guid contentItemId, string? publicId = "cloud-public-id")
    {
        var asset = new VisualAsset
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            ContentItemId = contentItemId,
            GenerationMode = GenerationMode.Campaign,
            Type = VisualAssetType.Image,
            FileUrl = "https://res.cloudinary.com/demo/image/upload/x.png",
            SourceType = VisualAssetSourceType.AiGenerated,
            PublicId = publicId,
            StorageProvider = publicId is null ? null : "Cloudinary",
            IsApproved = false,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.VisualAssets.Add(asset);
        return asset;
    }

    private static ScheduledPost AddScheduledPost(
        AppDbContext dbContext, Guid campaignId, Guid contentItemId, Guid socialAccountId, Guid brandProfileId,
        ScheduledPostStatus status, string? postId = null)
    {
        var post = new ScheduledPost
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            ContentItemId = contentItemId,
            SocialAccountId = socialAccountId,
            BrandProfileId = brandProfileId,
            ScheduledAt = DateTime.UtcNow.AddHours(2),
            Status = status,
            PostId = postId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.ScheduledPosts.Add(post);
        return post;
    }

    private static SocialAccount AddSocialAccount(AppDbContext dbContext, Guid brandProfileId, SocialPlatform platform)
    {
        var account = new SocialAccount
        {
            Id = Guid.NewGuid(),
            BrandProfileId = brandProfileId,
            Platform = platform,
            AccountName = "Test Page",
            AccountIdExternal = "ext-123",
            Token = "encrypted-token",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.SocialAccounts.Add(account);
        return account;
    }

    private static (ICurrentTenantContext Tenant, ICurrentUserService User) BuildContext(Guid tenantId)
    {
        var currentTenantContext = Substitute.For<ICurrentTenantContext>();
        currentTenantContext.TenantId.Returns(tenantId);
        currentTenantContext.Role.Returns(TenantMemberRole.Admin);

        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns(Guid.NewGuid());

        return (currentTenantContext, currentUserService);
    }

    private static DeleteCampaignCommandHandler BuildHandler(AppDbContext dbContext, Guid tenantId)
    {
        var (tenant, user) = BuildContext(tenantId);
        return new DeleteCampaignCommandHandler(dbContext, user, tenant);
    }

    /// <summary>Runs the background half of the delete against the queued outbox message, the way
    /// <c>CampaignCleanupHostedService</c> does — so these tests exercise the real payload the
    /// command wrote rather than a hand-built one.</summary>
    private static async Task RunCleanupAsync(
        AppDbContext dbContext, ISocialPublisher? publisher = null, IMediaStorageService? mediaStorageService = null)
    {
        var tokenEncryptor = Substitute.For<ITokenEncryptor>();
        tokenEncryptor.Decrypt(Arg.Any<string>()).Returns("plain-token");

        var publishers = publisher is null ? [] : new[] { publisher };
        var storage = mediaStorageService ?? Substitute.For<IMediaStorageService>();

        foreach (var payload in QueuedCleanups(dbContext))
        {
            await CampaignCleanupExecutor.ExecuteAsync(
                dbContext, tokenEncryptor, publishers, storage, payload, CancellationToken.None);
        }
    }

    private static List<CampaignCleanupPayload> QueuedCleanups(AppDbContext dbContext) =>
        dbContext.OutboxMessages
            .Where(m => m.Type == CampaignCleanupPublisher.CampaignCleanupType && m.ProcessedAt == null)
            .AsEnumerable()
            .Select(m => JsonSerializer.Deserialize<CampaignCleanupPayload>(m.PayloadJson)!)
            .ToList();

    [Fact]
    public async Task Delete_SoftDeletesCampaignContentAndImages_AndExcludesFromGetCampaigns()
    {
        var (dbContext, tenantId, brandProfileId, campaignId) = await SeedAsync();
        var item = AddContentItem(dbContext, campaignId, brandProfileId, tenantId);
        AddVisualAsset(dbContext, campaignId, item.Id, publicId: null);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = BuildHandler(dbContext, tenantId);
        var result = await handler.Handle(new DeleteCampaignCommand(campaignId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Data!.ContentItemsDeleted);
        Assert.Equal(1, result.Data.ImagesDeleted);

        var (tenant, user) = BuildContext(tenantId);
        var list = await new GetCampaignsQueryHandler(dbContext, tenant, user)
            .Handle(new GetCampaignsQuery(brandProfileId, IncludeArchived: true), CancellationToken.None);
        Assert.Empty(list.Data!.Items);

        Assert.Empty(dbContext.ContentItems.Where(c => c.CampaignId == campaignId));
        Assert.Empty(dbContext.VisualAssets.Where(v => v.CampaignId == campaignId));
    }

    [Fact]
    public async Task Delete_CancelsPendingScheduledPost_ViaNativePublisher()
    {
        var (dbContext, tenantId, brandProfileId, campaignId) = await SeedAsync();
        var item = AddContentItem(dbContext, campaignId, brandProfileId, tenantId);
        var account = AddSocialAccount(dbContext, brandProfileId, SocialPlatform.Facebook);
        var post = AddScheduledPost(dbContext, campaignId, item.Id, account.Id, brandProfileId, ScheduledPostStatus.Pending, postId: "fb-post-1");
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var publisher = Substitute.For<ISocialPublisher>();
        publisher.Platform.Returns(SocialPlatform.Facebook);
        publisher.SupportsNativeScheduling.Returns(true);
        publisher.CancelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(PublishResult.Success("fb-post-1"));

        var handler = BuildHandler(dbContext, tenantId);
        var result = await handler.Handle(new DeleteCampaignCommand(campaignId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Data!.ScheduledPostsCancelled);

        // The request itself must not wait on Facebook — that latency is the whole reason this moved
        // to the outbox. The local row is already Cancelled, so Rawaj's own poller won't fire it.
        await publisher.DidNotReceive().CancelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        var reloaded = dbContext.ChangeTracker.Entries<ScheduledPost>().First(e => e.Entity.Id == post.Id).Entity;
        Assert.Equal(ScheduledPostStatus.Cancelled, reloaded.Status);
        Assert.True(reloaded.IsDeleted);

        // ...and the platform-side revoke does happen, once the queued cleanup runs.
        await RunCleanupAsync(dbContext, publisher);
        await publisher.Received(1).CancelAsync("plain-token", "fb-post-1", Arg.Any<CancellationToken>());
        Assert.Null(reloaded.PostId);
    }

    [Fact]
    public async Task Cleanup_IsIdempotent_WhenTheSameMessageRunsTwice()
    {
        var (dbContext, tenantId, brandProfileId, campaignId) = await SeedAsync();
        var item = AddContentItem(dbContext, campaignId, brandProfileId, tenantId);
        var account = AddSocialAccount(dbContext, brandProfileId, SocialPlatform.Facebook);
        AddScheduledPost(dbContext, campaignId, item.Id, account.Id, brandProfileId, ScheduledPostStatus.Pending, postId: "fb-post-1");
        AddVisualAsset(dbContext, campaignId, item.Id, publicId: "campaigns/abc123");
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var publisher = Substitute.For<ISocialPublisher>();
        publisher.Platform.Returns(SocialPlatform.Facebook);
        publisher.SupportsNativeScheduling.Returns(true);
        publisher.CancelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(PublishResult.Success("fb-post-1"));

        var storage = Substitute.For<IMediaStorageService>();
        storage.IsConfigured.Returns(true);

        await BuildHandler(dbContext, tenantId).Handle(new DeleteCampaignCommand(campaignId), CancellationToken.None);

        // The outbox retries a whole message on failure, so a partly-finished cleanup will be re-run.
        // Revoking twice would be an error against the platform; clearing PostId is what prevents it.
        await RunCleanupAsync(dbContext, publisher, storage);
        await RunCleanupAsync(dbContext, publisher, storage);

        await publisher.Received(1).CancelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_LeavesPublishedPostLive_OnlySoftDeletesLocalRecord()
    {
        var (dbContext, tenantId, brandProfileId, campaignId) = await SeedAsync();
        var item = AddContentItem(dbContext, campaignId, brandProfileId, tenantId);
        var account = AddSocialAccount(dbContext, brandProfileId, SocialPlatform.Facebook);
        var post = AddScheduledPost(dbContext, campaignId, item.Id, account.Id, brandProfileId, ScheduledPostStatus.Published, postId: "fb-post-live");
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var publisher = Substitute.For<ISocialPublisher>();
        publisher.Platform.Returns(SocialPlatform.Facebook);
        publisher.SupportsNativeScheduling.Returns(true);

        var handler = BuildHandler(dbContext, tenantId);
        var result = await handler.Handle(new DeleteCampaignCommand(campaignId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Data!.PublishedPostsLeftLive);
        Assert.Equal(0, result.Data.ScheduledPostsCancelled);

        // The whole point: a published post is never handed to CancelAsync — it must stay live. That
        // has to hold after the background cleanup runs too, not just for the request itself.
        await RunCleanupAsync(dbContext, publisher);
        await publisher.DidNotReceive().CancelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        var reloaded = dbContext.ChangeTracker.Entries<ScheduledPost>().First(e => e.Entity.Id == post.Id).Entity;
        Assert.Equal(ScheduledPostStatus.Published, reloaded.Status);
        Assert.Equal("fb-post-live", reloaded.PostId);
        Assert.True(reloaded.IsDeleted);
    }

    [Fact]
    public async Task Delete_WhenNativeCancelFails_StillDeletesTheCampaign()
    {
        var (dbContext, tenantId, brandProfileId, campaignId) = await SeedAsync();
        var item = AddContentItem(dbContext, campaignId, brandProfileId, tenantId);
        var account = AddSocialAccount(dbContext, brandProfileId, SocialPlatform.Facebook);
        AddScheduledPost(dbContext, campaignId, item.Id, account.Id, brandProfileId, ScheduledPostStatus.Pending, postId: "fb-post-flaky");
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var publisher = Substitute.For<ISocialPublisher>();
        publisher.Platform.Returns(SocialPlatform.Facebook);
        publisher.SupportsNativeScheduling.Returns(true);
        publisher.CancelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(PublishResult.Failure("Facebook API is down"));

        var handler = BuildHandler(dbContext, tenantId);
        var result = await handler.Handle(new DeleteCampaignCommand(campaignId), CancellationToken.None);

        // A flaky platform call must not strand the whole campaign undeleted — and now it can't even
        // be observed by the request, which is gone long before the cleanup is attempted.
        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Data!.ScheduledPostsCancelled);

        var campaign = await dbContext.MarketingCampaigns.IgnoreQueryFilters()
            .FirstAsync(c => c.Id == campaignId);
        Assert.True(campaign.IsDeleted);

        // The cleanup surfaces the failure by throwing, which is what hands the message back to the
        // outbox to retry rather than declaring a still-scheduled post successfully revoked.
        await Assert.ThrowsAsync<InvalidOperationException>(() => RunCleanupAsync(dbContext, publisher));
    }

    [Fact]
    public async Task Delete_PurgesConfiguredCloudinaryAssets_FromStorage()
    {
        var (dbContext, tenantId, brandProfileId, campaignId) = await SeedAsync();
        var item = AddContentItem(dbContext, campaignId, brandProfileId, tenantId);
        AddVisualAsset(dbContext, campaignId, item.Id, publicId: "campaigns/abc123");
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var storage = Substitute.For<IMediaStorageService>();
        storage.IsConfigured.Returns(true);

        var handler = BuildHandler(dbContext, tenantId);
        var result = await handler.Handle(new DeleteCampaignCommand(campaignId), CancellationToken.None);

        Assert.True(result.Succeeded);
        // Purging N images was N sequential Cloudinary round-trips on the request thread before this
        // moved to the outbox — the single biggest contributor to how long the modal used to hang.
        await storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        await RunCleanupAsync(dbContext, mediaStorageService: storage);
        await storage.Received(1).DeleteAsync("campaigns/abc123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_QueuesExactlyOneCleanupMessage_CarryingTheWorkToDoLater()
    {
        var (dbContext, tenantId, brandProfileId, campaignId) = await SeedAsync();
        var item = AddContentItem(dbContext, campaignId, brandProfileId, tenantId);
        var account = AddSocialAccount(dbContext, brandProfileId, SocialPlatform.Facebook);
        var pending = AddScheduledPost(dbContext, campaignId, item.Id, account.Id, brandProfileId, ScheduledPostStatus.Pending, postId: "fb-1");
        // No PostId: never handed to the platform, so there is nothing to revoke and it must not be
        // queued — otherwise every locally-scheduled post would cost a pointless background lookup.
        AddScheduledPost(dbContext, campaignId, item.Id, account.Id, brandProfileId, ScheduledPostStatus.Pending);
        AddVisualAsset(dbContext, campaignId, item.Id, publicId: "campaigns/a");
        AddVisualAsset(dbContext, campaignId, item.Id, publicId: null);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        await BuildHandler(dbContext, tenantId).Handle(new DeleteCampaignCommand(campaignId), CancellationToken.None);

        var queued = Assert.Single(QueuedCleanups(dbContext));
        Assert.Equal(campaignId, queued.CampaignId);
        Assert.Equal([pending.Id], queued.PendingScheduledPostIds);
        Assert.Equal(["campaigns/a"], queued.MediaPublicIds);
    }

    [Fact]
    public async Task Delete_WithNothingExternalToCleanUp_QueuesNoMessage()
    {
        var (dbContext, tenantId, brandProfileId, campaignId) = await SeedAsync();
        var item = AddContentItem(dbContext, campaignId, brandProfileId, tenantId);
        AddVisualAsset(dbContext, campaignId, item.Id, publicId: null);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        await BuildHandler(dbContext, tenantId).Handle(new DeleteCampaignCommand(campaignId), CancellationToken.None);

        Assert.Empty(QueuedCleanups(dbContext));
    }

    [Fact]
    public async Task Delete_FromAnotherTenant_ReturnsNotFound()
    {
        var (dbContext, _, _, campaignId) = await SeedAsync();

        var handler = BuildHandler(dbContext, Guid.NewGuid());
        var result = await handler.Handle(new DeleteCampaignCommand(campaignId), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Campaign not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteSummary_ReportsAccurateCounts()
    {
        var (dbContext, tenantId, brandProfileId, campaignId) = await SeedAsync();
        var item1 = AddContentItem(dbContext, campaignId, brandProfileId, tenantId);
        var item2 = AddContentItem(dbContext, campaignId, brandProfileId, tenantId);
        AddVisualAsset(dbContext, campaignId, item1.Id);
        var account = AddSocialAccount(dbContext, brandProfileId, SocialPlatform.Facebook);
        AddScheduledPost(dbContext, campaignId, item1.Id, account.Id, brandProfileId, ScheduledPostStatus.Pending);
        AddScheduledPost(dbContext, campaignId, item2.Id, account.Id, brandProfileId, ScheduledPostStatus.Published);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var currentTenantContext = Substitute.For<ICurrentTenantContext>();
        currentTenantContext.TenantId.Returns(tenantId);
        var handler = new GetCampaignDeleteSummaryQueryHandler(dbContext, currentTenantContext);

        var result = await handler.Handle(new GetCampaignDeleteSummaryQuery(campaignId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Summer Sale", result.Data!.Name);
        Assert.Equal(2, result.Data.ContentItemCount);
        Assert.Equal(1, result.Data.ImageCount);
        Assert.Equal(1, result.Data.PendingScheduledCount);
        Assert.Equal(1, result.Data.PublishedScheduledCount);
    }
}
