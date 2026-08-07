using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Services;
using Rawaj.Application.Features.AiPipeline.Executors;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Domain.ValueObjects;
using Rawaj.Persistence;
using Xunit;

namespace Rawaj.Application.Tests.Features.AiPipeline;

/// <summary>
/// The content stages — the split that lets one post's image be retried without regenerating the
/// batch, or leaving the eight good posts behind while the ninth's image is stuck.
/// </summary>
public class ContentExecutorTests
{
    private const string ValidContentPlan = """
        {"posts":[
          {"platform":"Instagram","contentType":"Post","dayOffset":0,"hour":12,"content":"منشور أول",
           "hashtags":["قهوة"],"cta":"اطلب الآن","imagePrompt":"a cup of coffee on a wooden table"},
          {"platform":"Instagram","contentType":"Post","dayOffset":1,"hour":9,"content":"منشور ثاني",
           "hashtags":[],"cta":null,"imagePrompt":"a bag of coffee beans"}
        ]}
        """;

    // ── ContentPlan ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ContentPlan_CreatesOneContentItemPerPost_AndFansOutTheirIds()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var executor = new ContentPlanExecutor(dbContext, TextServiceReturning(ValidContentPlan), new PromptTemplateProvider());

        var result = await executor.ExecuteAsync(world.Context(AiPipelineStageKind.ContentPlan), CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.FanOutTargets);
        Assert.Equal(2, result.FanOutTargets!.Count);

        var items = await dbContext.ContentItems.Where(c => c.CampaignId == world.Campaign.Id).ToListAsync();
        Assert.Equal(2, items.Count);
        Assert.All(items, i => Assert.Equal(world.Stage.Id, i.PipelineStageId));
        Assert.All(items, i => Assert.Equal(ContentStatus.Draft, i.Status));
        Assert.Equal(result.FanOutTargets!.OrderBy(id => id), items.Select(i => i.Id).OrderBy(id => id));
    }

    [Fact]
    public async Task ContentPlan_PassesTheApprovedStrategyAndBrief_NotJustBrandIdentity()
    {
        // The whole point of gating content on approval: posts must actually follow what was
        // reviewed and paid for, not merely the brand's identity fields.
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var textService = TextServiceReturning(ValidContentPlan);
        var executor = new ContentPlanExecutor(dbContext, textService, new PromptTemplateProvider());

        var context = world.Context(
            AiPipelineStageKind.ContentPlan,
            inputs: new Dictionary<AiArtifactKind, string> { [AiArtifactKind.Strategy] = """{"executiveSummary":"ركز على الجودة"}""" });

        await executor.ExecuteAsync(context, CancellationToken.None);

        await textService.Received(1).GenerateTextAsync(
            Arg.Is<string>(p => p != null && p.Contains("ركز على الجودة") && p.Contains("reviewed and approved")),
            Arg.Any<CancellationToken>(), Arg.Any<AiTextGenerationOptions>());
    }

    [Fact]
    public async Task ContentPlan_Fails_WhenNoPostParses()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var executor = new ContentPlanExecutor(dbContext, TextServiceReturning("""{"posts":[]}"""), new PromptTemplateProvider());

        var result = await executor.ExecuteAsync(world.Context(AiPipelineStageKind.ContentPlan), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(AiFailureKind.Validation, result.FailureKind);
    }

    [Fact]
    public async Task ContentPlan_RejectsOutputMissingThePostsField()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var executor = new ContentPlanExecutor(dbContext, TextServiceReturning("""{"somethingElse":true}"""), new PromptTemplateProvider());

        var result = await executor.ExecuteAsync(world.Context(AiPipelineStageKind.ContentPlan), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(AiFailureKind.Validation, result.FailureKind);
    }

    // ── ContentImage ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ContentImage_WritesAnAiGeneratedVisualAsset_ForTheTargetedPost()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var item = await world.AddContentItemAsync(dbContext);

        var imageService = Substitute.For<IAiImageGenerationService>();
        imageService.GenerateImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AiImageGenerationResult.Success([1, 2, 3], "image/png"));
        var mediaStorage = Substitute.For<IMediaStorageService>();
        mediaStorage.IsConfigured.Returns(false);

        var executor = new ContentImageExecutor(dbContext, imageService, mediaStorage);
        var context = world.Context(AiPipelineStageKind.ContentImage, targetRefId: item.Id);

        var result = await executor.ExecuteAsync(context, CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Null(result.ArtifactJson);

        var asset = await dbContext.VisualAssets.SingleAsync(a => a.ContentItemId == item.Id);
        Assert.Equal(VisualAssetSourceType.AiGenerated, asset.SourceType);
        Assert.StartsWith("data:image/png;base64,", asset.FileUrl);
    }

    [Fact]
    public async Task ContentImage_PrefersTheModelAuthoredEnglishPrompt_OverThePostCopy()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var item = await world.AddContentItemAsync(dbContext, imagePrompt: "a cup of coffee on a wooden table");

        var imageService = Substitute.For<IAiImageGenerationService>();
        imageService.GenerateImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AiImageGenerationResult.Success([1], "image/png"));
        var mediaStorage = Substitute.For<IMediaStorageService>();
        mediaStorage.IsConfigured.Returns(false);

        var executor = new ContentImageExecutor(dbContext, imageService, mediaStorage);
        await executor.ExecuteAsync(world.Context(AiPipelineStageKind.ContentImage, targetRefId: item.Id), CancellationToken.None);

        await imageService.Received(1).GenerateImageAsync(
            Arg.Is<string>(p => p != null && p.Contains("a cup of coffee on a wooden table")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ContentImage_ReportsFailure_AndWritesNoPlaceholder_LeavingThatToTheOrchestrator()
    {
        // The placeholder is only correct once every retry is exhausted — a call this executor
        // cannot make on its own. It must fail cleanly and leave the decision to the caller.
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var item = await world.AddContentItemAsync(dbContext);

        var imageService = Substitute.For<IAiImageGenerationService>();
        imageService.GenerateImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AiImageGenerationResult.Failure("model overloaded"));
        var mediaStorage = Substitute.For<IMediaStorageService>();

        var executor = new ContentImageExecutor(dbContext, imageService, mediaStorage);
        var result = await executor.ExecuteAsync(world.Context(AiPipelineStageKind.ContentImage, targetRefId: item.Id), CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.False(await dbContext.VisualAssets.AnyAsync(a => a.ContentItemId == item.Id));
    }

    [Fact]
    public async Task ContentImage_LogsTheProviderCall_EvenOnFailure()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var item = await world.AddContentItemAsync(dbContext);

        var imageService = Substitute.For<IAiImageGenerationService>();
        imageService.GenerateImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AiImageGenerationResult.Failure("quota exceeded"));
        var mediaStorage = Substitute.For<IMediaStorageService>();

        var executor = new ContentImageExecutor(dbContext, imageService, mediaStorage);
        await executor.ExecuteAsync(world.Context(AiPipelineStageKind.ContentImage, targetRefId: item.Id), CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var job = await dbContext.AiJobs.SingleAsync();
        Assert.Equal("Cloudflare", job.Provider);
        Assert.Equal(AiJobStatus.Failed, job.Status);
    }

    [Fact]
    public async Task ContentImage_Fails_WhenTheTargetedPostNoLongerExists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var executor = new ContentImageExecutor(
            dbContext, Substitute.For<IAiImageGenerationService>(), Substitute.For<IMediaStorageService>());

        var result = await executor.ExecuteAsync(
            world.Context(AiPipelineStageKind.ContentImage, targetRefId: Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(AiFailureKind.Validation, result.FailureKind);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────

    private static IAiTextGenerationService TextServiceReturning(string text)
    {
        var service = Substitute.For<IAiTextGenerationService>();
        service.GenerateTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<AiTextGenerationOptions>())
            .Returns(AiTextGenerationResult.Success(text, 500, "test-model", 1234));
        return service;
    }

    private sealed record World(Tenant Tenant, TenantBrandProfile Brand, MarketingCampaign Campaign, AiPipelineRun Run)
    {
        public AiPipelineStage Stage { get; private set; } = null!;

        public StageContext Context(
            AiPipelineStageKind kind,
            IReadOnlyDictionary<AiArtifactKind, string>? inputs = null,
            bool repairPrompt = false,
            Guid? targetRefId = null)
        {
            Stage = new AiPipelineStage
            {
                Id = Guid.NewGuid(),
                RunId = Run.Id,
                Kind = kind,
                Status = AiPipelineStageStatus.Running,
                TargetRefId = targetRefId,
                CreatedAt = DateTime.UtcNow
            };

            return new StageContext(
                Run, Stage, Brand, Campaign,
                inputs ?? new Dictionary<AiArtifactKind, string>(),
                Run.TriggeredBy, TenantMemberRole.Owner, repairPrompt);
        }

        public async Task<ContentItem> AddContentItemAsync(AppDbContext dbContext, string? imagePrompt = null)
        {
            var now = DateTime.UtcNow;
            var item = new ContentItem
            {
                Id = Guid.NewGuid(),
                CampaignId = Campaign.Id,
                TenantId = Tenant.Id,
                BrandProfileId = Brand.Id,
                CreatedBy = Run.TriggeredBy,
                GenerationMode = GenerationMode.Campaign,
                ContentType = ContentType.Post,
                Platform = SocialPlatform.Instagram,
                Language = Language.Ar,
                Content = "منشور",
                ImagePrompt = imagePrompt,
                Status = ContentStatus.Draft,
                CreatedAt = now,
                UpdatedAt = now
            };
            dbContext.ContentItems.Add(item);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            return item;
        }
    }

    private static async Task<World> SeedAsync(AppDbContext dbContext)
    {
        var now = DateTime.UtcNow;
        var planId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        dbContext.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId, Name = "Free", Cost = 0, Currency = "USD",
            MaxCampaignsMonthly = 100, IsActive = true, CreatedAt = now
        });

        dbContext.Subscriptions.Add(new Subscription
        {
            Id = subscriptionId, SubscriptionPlanId = planId, Status = SubscriptionStatus.Active,
            CurrentPeriodStart = now, CurrentPeriodEnd = now.AddMonths(1), CreatedAt = now
        });

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(), Name = "Test Tenant", Subdomain = Guid.NewGuid().ToString("N"),
            TenantType = TenantType.Business, OwnerUserId = Guid.NewGuid(), SubscriptionId = subscriptionId,
            CoinBalance = 100_000, IsActive = true, IsActivated = true, CreatedAt = now, UpdatedAt = now
        };
        dbContext.Tenants.Add(tenant);

        var brand = new TenantBrandProfile
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, Name = "Qahwa",
            Description = "Specialty coffee roaster",
            BrandInfo = new BrandInfo { Industry = "Coffee" },
            Status = BrandProfileStatus.Active, CreatedAt = now, UpdatedAt = now
        };
        dbContext.TenantBrandProfiles.Add(brand);

        var campaign = new MarketingCampaign
        {
            Id = Guid.NewGuid(), BrandProfileId = brand.Id, CreatedBy = Guid.NewGuid(),
            Name = "Summer Push", Objective = "awareness", TargetPlatforms = ["Instagram"],
            PlanApprovedAt = now,
            Status = CampaignStatus.Active, CreatedAt = now, UpdatedAt = now
        };
        dbContext.MarketingCampaigns.Add(campaign);

        var run = new AiPipelineRun
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BrandProfileId = brand.Id, CampaignId = campaign.Id,
            TriggeredBy = Guid.NewGuid(), Status = AiPipelineRunStatus.Running, CreatedAt = now, UpdatedAt = now
        };
        dbContext.AiPipelineRuns.Add(run);

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return new World(tenant, brand, campaign, run);
    }
}
