using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Services;
using Rawaj.Application.Features.AiPipeline.Executors;
using Rawaj.Application.Features.AiPipeline.Services;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Persistence;
using Xunit;

namespace Rawaj.Application.Tests.Features.AiPipeline;

/// <summary>
/// The orchestrator, driven synchronously against real executors and a scripted provider layer — the
/// point is proving the graph-walking logic itself: a run advances through waves of dependent stages,
/// parks correctly on a person or an empty wallet, resumes once the block clears, and an optional
/// stage exhausting its retries doesn't take the run down with it.
///
/// <para>No worker, no lease reclaim, no real time passing — that is C16. Backoff is bypassed here by
/// rewinding a stage's <c>NextAttemptAt</c> directly, the same effect a real clock would eventually
/// have.</para>
/// </summary>
public class PipelineOrchestratorTests
{
    private const string BrandAnalysisJson = """
        {"businessSummary":"ملخص","identityCore":"هوية","voiceProfile":{"tone":"ودود"},
         "valuePropositions":["جودة"],"differentiators":["محلي"]}
        """;

    private const string CampaignAnalysisJson = """
        {"objectiveClassification":"وعي","audienceSegments":[{"name":"طلاب"}],"successCriteria":["وصول"],
         "researchQueries":{"market":["اتجاهات القهوة"],"competitor":["منافسين القهوة"]}}
        """;

    private const string MarketResearchJson = """{"trends":["نمو"],"sources":[{"title":"t","url":"u"}]}""";
    private const string CompetitorResearchJson = """{"competitors":[{"name":"c"}],"sources":[{"title":"t","url":"u"}]}""";
    private const string PositioningJson = """{"businessAndMarketAnalysis":"a","brandStrategy":"b","marketingStrategy":"c"}""";
    private const string BlueprintJson = """{"campaignBlueprint":{"pillars":["جودة"]}}""";
    private const string RoadmapJson = """
        {"executiveSummary":"ملخص","contentProductionPlan":"خطة","executionRoadmap":"تنفيذ","aiRecommendations":[]}
        """;

    private const string TwoPostsJson = """
        {"posts":[
          {"platform":"Instagram","contentType":"Post","dayOffset":0,"hour":12,"content":"منشور أول","hashtags":[],"imagePrompt":"a coffee cup"},
          {"platform":"Instagram","contentType":"Post","dayOffset":1,"hour":9,"content":"منشور ثاني","hashtags":[],"imagePrompt":"coffee beans"}
        ]}
        """;

    private const string OnePostJson = """
        {"posts":[{"platform":"Instagram","contentType":"Post","dayOffset":0,"hour":12,"content":"منشور","hashtags":[],"imagePrompt":"a coffee cup"}]}
        """;

    private sealed class TestCoinCosts : ICoinCostProvider
    {
        public int ContentGeneration => 200;
        public int VisualGeneration => 400;
        public int CampaignContentGeneration => 1_000;
        public int MarketingPlanGeneration => 12_000;
        public int Scheduling => 100;
        public int BusinessDiagnosis => 4_000;
        public int CompetitiveAnalysis => 3_500;
        public int ReasoningConversation => 2_500;
    }

    private static readonly TestCoinCosts Costs = new();

    // ── Full run ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task FullRun_ParksForApproval_ThenCompletesWithContentAndImages_AfterApproval()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext, coinBalance: 100_000, freeMarketingPlanUsed: false);
        var text = DefaultTextService(TwoPostsJson);
        var orchestrator = world.BuildOrchestrator(dbContext, text);

        var run = await orchestrator.StartAsync(world.Tenant.Id, world.Brand.Id, world.Campaign.Id, world.UserId, CancellationToken.None);
        await orchestrator.AdvanceAsync(run, TenantMemberRole.Owner, CancellationToken.None);

        Assert.Equal(AiPipelineRunStatus.AwaitingApproval, run.Status);

        var campaign = await dbContext.MarketingCampaigns.FirstAsync(c => c.Id == world.Campaign.Id);
        var approval = new PipelineApprovalService(dbContext);
        var approved = await approval.ApproveAsync(run, campaign, CancellationToken.None);
        Assert.True(approved.Succeeded);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        await orchestrator.AdvanceAsync(run, TenantMemberRole.Owner, CancellationToken.None);

        Assert.Equal(AiPipelineRunStatus.Completed, run.Status);

        var stages = await dbContext.AiPipelineStages.Where(s => s.RunId == run.Id).ToListAsync();
        Assert.All(stages, s => Assert.True(s.Status is AiPipelineStageStatus.Completed or AiPipelineStageStatus.Skipped));

        var items = await dbContext.ContentItems.Where(c => c.CampaignId == world.Campaign.Id).ToListAsync();
        Assert.Equal(2, items.Count);

        var assets = await dbContext.VisualAssets.Where(v => v.ContentItemId != null).ToListAsync();
        Assert.Equal(2, assets.Count);
        Assert.All(assets, a => Assert.Equal(VisualAssetSourceType.AiGenerated, a.SourceType));

        // 4,000 (business diagnosis) + 3,500 (competitor research found data) + 0 (first strategy is
        // free) + 1,000 (content) = 8,500, not the 20,500 sticker price for a paid-strategy tenant —
        // proving the split into eleven stages still resolves to the one-charge-per-group total.
        Assert.Equal(8_500, run.TotalCoinsSpent);
    }

    [Fact]
    public async Task BrandOnlyRun_StartsOnlyBrandAnalysis()
    {
        // No campaign to run the rest of the graph against — starting the full graph would just burn
        // attempts on stages that can never succeed.
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext, coinBalance: 100_000, freeMarketingPlanUsed: false);
        var orchestrator = world.BuildOrchestrator(dbContext, DefaultTextService(TwoPostsJson));

        var run = await orchestrator.StartAsync(world.Tenant.Id, world.Brand.Id, null, world.UserId, CancellationToken.None);
        await orchestrator.AdvanceAsync(run, TenantMemberRole.Owner, CancellationToken.None);

        Assert.Equal(AiPipelineRunStatus.Completed, run.Status);
        var stages = await dbContext.AiPipelineStages.Where(s => s.RunId == run.Id).ToListAsync();
        Assert.Single(stages);
        Assert.Equal(AiPipelineStageKind.BrandAnalysis, stages[0].Kind);
    }

    // ── Mid-run failure and resume ──────────────────────────────────────────────────────────

    [Fact]
    public async Task TransientFailure_ParksTheStageOnBackoff_AndResumesOnceItPasses()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext, coinBalance: 100_000, freeMarketingPlanUsed: false);
        var text = DefaultTextService(TwoPostsJson);
        text.Enqueue("analysing the campaign", null, CampaignAnalysisJson); // fails once, then succeeds
        var orchestrator = world.BuildOrchestrator(dbContext, text);

        var run = await orchestrator.StartAsync(world.Tenant.Id, world.Brand.Id, world.Campaign.Id, world.UserId, CancellationToken.None);
        await orchestrator.AdvanceAsync(run, TenantMemberRole.Owner, CancellationToken.None);

        var campaignAnalysis = await dbContext.AiPipelineStages
            .SingleAsync(s => s.RunId == run.Id && s.Kind == AiPipelineStageKind.CampaignAnalysis);

        Assert.Equal(AiPipelineStageStatus.Pending, campaignAnalysis.Status);
        Assert.Equal(1, campaignAnalysis.Attempts);
        Assert.NotNull(campaignAnalysis.NextAttemptAt);
        Assert.Equal(AiPipelineRunStatus.Running, run.Status);

        // A crashed worker or a still-running backoff looks the same from here: call AdvanceAsync
        // again once the clock has actually passed the stage's NextAttemptAt.
        await RewindBackoffAsync(dbContext, campaignAnalysis.Id);
        await orchestrator.AdvanceAsync(run, TenantMemberRole.Owner, CancellationToken.None);

        campaignAnalysis = await dbContext.AiPipelineStages.SingleAsync(s => s.Id == campaignAnalysis.Id);
        Assert.Equal(AiPipelineStageStatus.Completed, campaignAnalysis.Status);
        Assert.Equal(1, campaignAnalysis.Attempts); // the retry succeeded; it consumed no further attempts
        Assert.Equal(AiPipelineRunStatus.AwaitingApproval, run.Status);
    }

    // ── Optional stage exhausting its retries ──────────────────────────────────────────────

    [Fact]
    public async Task OptionalStage_ExhaustingEveryRetry_SkipsWithoutFailingTheRun()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext, coinBalance: 100_000, freeMarketingPlanUsed: false);
        var text = DefaultTextService(TwoPostsJson);
        // Search succeeds (so this exercises the synthesis-call failure path, not the
        // already-covered "nothing found" best-effort path), but the model never returns anything
        // usable — three attempts, all bad.
        text.Enqueue("competitive analyst identifying", null, null, null);
        var orchestrator = world.BuildOrchestrator(dbContext, text);

        var run = await orchestrator.StartAsync(world.Tenant.Id, world.Brand.Id, world.Campaign.Id, world.UserId, CancellationToken.None);
        await orchestrator.AdvanceAsync(run, TenantMemberRole.Owner, CancellationToken.None);

        var research = await dbContext.AiPipelineStages
            .SingleAsync(s => s.RunId == run.Id && s.Kind == AiPipelineStageKind.CompetitorResearch);
        Assert.Equal(AiPipelineStageStatus.Pending, research.Status);
        Assert.Equal(1, research.Attempts);

        await RewindBackoffAsync(dbContext, research.Id);
        await orchestrator.AdvanceAsync(run, TenantMemberRole.Owner, CancellationToken.None);
        research = await dbContext.AiPipelineStages.SingleAsync(s => s.Id == research.Id);
        Assert.Equal(AiPipelineStageStatus.Pending, research.Status);
        Assert.Equal(2, research.Attempts);

        await RewindBackoffAsync(dbContext, research.Id);
        await orchestrator.AdvanceAsync(run, TenantMemberRole.Owner, CancellationToken.None);
        research = await dbContext.AiPipelineStages.SingleAsync(s => s.Id == research.Id);

        Assert.Equal(AiPipelineStageStatus.Skipped, research.Status);
        Assert.Equal(0, research.CoinsCharged);
        // The run is not dragged down by it — everything that only needed CompetitorResearch to be
        // terminal (Skipped satisfies a dependency exactly as Completed does) keeps moving.
        Assert.Equal(AiPipelineRunStatus.AwaitingApproval, run.Status);
    }

    // ── Coin exhaustion ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CoinShortfall_ParksTheRun_ConsumesNoAttempt_AndTopUpResumes()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext, coinBalance: 100, freeMarketingPlanUsed: false);
        var text = DefaultTextService(TwoPostsJson);
        var orchestrator = world.BuildOrchestrator(dbContext, text);

        var run = await orchestrator.StartAsync(world.Tenant.Id, world.Brand.Id, world.Campaign.Id, world.UserId, CancellationToken.None);
        await orchestrator.AdvanceAsync(run, TenantMemberRole.Owner, CancellationToken.None);

        Assert.Equal(AiPipelineRunStatus.AwaitingCoins, run.Status);

        var campaignAnalysis = await dbContext.AiPipelineStages
            .SingleAsync(s => s.RunId == run.Id && s.Kind == AiPipelineStageKind.CampaignAnalysis);
        Assert.Equal(AiPipelineStageStatus.Pending, campaignAnalysis.Status);
        Assert.Equal(0, campaignAnalysis.Attempts); // nothing was attempted — nothing to consume
        Assert.Equal(AiFailureKind.Coins, campaignAnalysis.LastErrorKind);

        var tenant = await dbContext.Tenants.FirstAsync(t => t.Id == world.Tenant.Id);
        tenant.CoinBalance = 100_000;
        await dbContext.SaveChangesAsync(CancellationToken.None);

        await orchestrator.AdvanceAsync(run, TenantMemberRole.Owner, CancellationToken.None);

        campaignAnalysis = await dbContext.AiPipelineStages.SingleAsync(s => s.Id == campaignAnalysis.Id);
        Assert.Equal(AiPipelineStageStatus.Completed, campaignAnalysis.Status);
        Assert.Equal(0, campaignAnalysis.Attempts);
        Assert.Equal(AiPipelineRunStatus.AwaitingApproval, run.Status);
    }

    // ── ContentImage placeholder on exhaustion ─────────────────────────────────────────────

    [Fact]
    public async Task ContentImage_ExhaustingEveryRetry_AttachesThePlaceholder_AndTheRunStillCompletes()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext, coinBalance: 100_000, freeMarketingPlanUsed: false);
        var text = DefaultTextService(OnePostJson);
        var image = new FakeImageService { AlwaysFail = true };
        var orchestrator = world.BuildOrchestrator(dbContext, text, image);

        var run = await orchestrator.StartAsync(world.Tenant.Id, world.Brand.Id, world.Campaign.Id, world.UserId, CancellationToken.None);
        await orchestrator.AdvanceAsync(run, TenantMemberRole.Owner, CancellationToken.None);

        var campaign = await dbContext.MarketingCampaigns.FirstAsync(c => c.Id == world.Campaign.Id);
        await new PipelineApprovalService(dbContext).ApproveAsync(run, campaign, CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        await orchestrator.AdvanceAsync(run, TenantMemberRole.Owner, CancellationToken.None);

        var image1 = await dbContext.AiPipelineStages.SingleAsync(s => s.RunId == run.Id && s.Kind == AiPipelineStageKind.ContentImage);
        Assert.Equal(AiPipelineStageStatus.Pending, image1.Status);

        await RewindBackoffAsync(dbContext, image1.Id);
        await orchestrator.AdvanceAsync(run, TenantMemberRole.Owner, CancellationToken.None);
        image1 = await dbContext.AiPipelineStages.SingleAsync(s => s.Id == image1.Id);
        Assert.Equal(AiPipelineStageStatus.Pending, image1.Status);

        await RewindBackoffAsync(dbContext, image1.Id);
        await orchestrator.AdvanceAsync(run, TenantMemberRole.Owner, CancellationToken.None);
        image1 = await dbContext.AiPipelineStages.SingleAsync(s => s.Id == image1.Id);

        Assert.Equal(AiPipelineStageStatus.Skipped, image1.Status);
        Assert.Equal(AiPipelineRunStatus.Completed, run.Status);

        var asset = await dbContext.VisualAssets.SingleAsync(v => v.ContentItemId == image1.TargetRefId);
        Assert.Equal(VisualAssetSourceType.Placeholder, asset.SourceType);
        Assert.Equal("/text-post.png", asset.FileUrl);
    }

    // ── Cancel ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Cancel_MarksTheRunCancelled_WithoutTouchingInFlightStages()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext, coinBalance: 100_000, freeMarketingPlanUsed: false);
        var orchestrator = world.BuildOrchestrator(dbContext, DefaultTextService(TwoPostsJson));

        var run = await orchestrator.StartAsync(world.Tenant.Id, world.Brand.Id, world.Campaign.Id, world.UserId, CancellationToken.None);
        await orchestrator.CancelAsync(run, CancellationToken.None);

        Assert.Equal(AiPipelineRunStatus.Cancelled, run.Status);
        var stages = await dbContext.AiPipelineStages.Where(s => s.RunId == run.Id).ToListAsync();
        Assert.All(stages, s => Assert.Equal(AiPipelineStageStatus.Pending, s.Status));
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────

    private static async Task RewindBackoffAsync(AppDbContext dbContext, Guid stageId)
    {
        var stage = await dbContext.AiPipelineStages.SingleAsync(s => s.Id == stageId);
        stage.NextAttemptAt = DateTime.UtcNow.AddSeconds(-1);
        await dbContext.SaveChangesAsync(CancellationToken.None);
    }

    private static FakeTextGenerationService DefaultTextService(string contentPlanJson)
    {
        var service = new FakeTextGenerationService();
        service.SetDefault("producing a durable profile", BrandAnalysisJson);
        service.SetDefault("analysing the campaign", CampaignAnalysisJson);
        service.SetDefault("market analyst studying the market", MarketResearchJson);
        service.SetDefault("competitive analyst identifying", CompetitorResearchJson);
        service.SetDefault("writing the positioning for the campaign", PositioningJson);
        service.SetDefault("designing the campaign blueprint", BlueprintJson);
        service.SetDefault("writing the execution roadmap", RoadmapJson);
        service.SetDefault("distinct social media posts", contentPlanJson);
        return service;
    }

    /// <summary>A scripted <see cref="IAiTextGenerationService"/>: each prompt is matched to a
    /// response by a substring marker unique to that stage's prompt, so one fake can stand in for
    /// eight different stages' calls. <see cref="Enqueue"/> overrides the default for one or more
    /// calls (null = a simulated failure), then falls back to the default for calls after that.</summary>
    private sealed class FakeTextGenerationService : IAiTextGenerationService
    {
        private readonly Dictionary<string, string> _defaults = new();
        private readonly Dictionary<string, Queue<string?>> _scripted = new();

        public void SetDefault(string marker, string json) => _defaults[marker] = json;

        public void Enqueue(string marker, params string?[] responses) =>
            _scripted[marker] = new Queue<string?>(responses);

        public Task<AiTextGenerationResult> GenerateTextAsync(
            string prompt, CancellationToken cancellationToken, AiTextGenerationOptions? options = null)
        {
            foreach (var (marker, queue) in _scripted)
            {
                if (!prompt.Contains(marker) || queue.Count == 0)
                {
                    continue;
                }

                var next = queue.Dequeue();
                return Task.FromResult(next is null
                    ? AiTextGenerationResult.Failure("simulated provider failure", "test-model", 10)
                    : AiTextGenerationResult.Success(next, 50, "test-model", 10));
            }

            foreach (var (marker, json) in _defaults)
            {
                if (prompt.Contains(marker))
                {
                    return Task.FromResult(AiTextGenerationResult.Success(json, 50, "test-model", 10));
                }
            }

            throw new InvalidOperationException("Unrecognised prompt in orchestrator test: " + prompt[..Math.Min(120, prompt.Length)]);
        }
    }

    private sealed class FakeSearchService : ITavilySearchService
    {
        public Task<TavilySearchResult> SearchAsync(string query, CancellationToken cancellationToken) =>
            Task.FromResult(TavilySearchResult.Success(
                "search summary", [new TavilySearchItem("Title", "https://example.com/" + Uri.EscapeDataString(query), "content about " + query)]));
    }

    private sealed class FakeImageService : IAiImageGenerationService
    {
        public bool AlwaysFail { get; init; }

        public Task<AiImageGenerationResult> GenerateImageAsync(string prompt, CancellationToken cancellationToken) =>
            Task.FromResult(AlwaysFail
                ? AiImageGenerationResult.Failure("simulated image failure")
                : AiImageGenerationResult.Success([1, 2, 3], "image/png"));
    }

    private sealed record World(Tenant Tenant, TenantBrandProfile Brand, MarketingCampaign Campaign, Guid UserId)
    {
        public IPipelineOrchestrator BuildOrchestrator(
            AppDbContext dbContext, FakeTextGenerationService text, FakeImageService? image = null)
        {
            var artifacts = new PipelineArtifactStore(dbContext);
            var templates = new PromptTemplateProvider();
            var search = new FakeSearchService();
            image ??= new FakeImageService();
            var mediaStorage = Substitute.For<IMediaStorageService>();
            mediaStorage.IsConfigured.Returns(false);

            var executors = new List<IPipelineStageExecutor>
            {
                new BrandAnalysisExecutor(dbContext, text, templates, artifacts),
                new CampaignAnalysisExecutor(dbContext, text, templates),
                new MarketResearchExecutor(dbContext, search, text, templates),
                new CompetitorResearchExecutor(dbContext, search, text, templates),
                new StrategyPositioningExecutor(dbContext, text, templates),
                new StrategyBlueprintExecutor(dbContext, text, templates),
                new StrategyRoadmapExecutor(dbContext, text, templates),
                new StrategyAssembleExecutor(),
                new ContentPlanExecutor(dbContext, text, templates),
                new ContentImageExecutor(dbContext, image, mediaStorage)
            };

            return new PipelineOrchestrator(dbContext, artifacts, Costs, executors);
        }
    }

    private static async Task<World> SeedAsync(AppDbContext dbContext, int coinBalance, bool freeMarketingPlanUsed)
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
            CoinBalance = coinBalance, FreeMarketingPlanUsed = freeMarketingPlanUsed,
            IsActive = true, IsActivated = true, CreatedAt = now, UpdatedAt = now
        };
        dbContext.Tenants.Add(tenant);

        var brand = new TenantBrandProfile
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, Name = "Qahwa", Description = "Specialty coffee roaster",
            Status = BrandProfileStatus.Active, CreatedAt = now, UpdatedAt = now
        };
        dbContext.TenantBrandProfiles.Add(brand);

        var campaign = new MarketingCampaign
        {
            Id = Guid.NewGuid(), BrandProfileId = brand.Id, CreatedBy = Guid.NewGuid(),
            Name = "Summer Push", Objective = "awareness", TargetPlatforms = ["Instagram"],
            Status = CampaignStatus.Draft, CreatedAt = now, UpdatedAt = now
        };
        dbContext.MarketingCampaigns.Add(campaign);

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return new World(tenant, brand, campaign, Guid.NewGuid());
    }
}
