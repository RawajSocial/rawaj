using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Services;
using Rawaj.Application.Features.AiPipeline.Executors;
using Rawaj.Application.Features.AiPipeline.Services;
using Rawaj.Infrastructure.Ai;
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
    public async Task FullRun_WritesThroughToTheLegacyCampaignColumns()
    {
        // Phase 6 compatibility: the existing strategy review UI and campaign-strategy-page read
        // campaign.CompetitorResearchJson/DiagnosisJson/AiPlanJson directly, not the new artifacts —
        // this is what keeps them working, unmodified, once C19 cuts the legacy endpoints over.
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext, coinBalance: 100_000, freeMarketingPlanUsed: false);
        var orchestrator = world.BuildOrchestrator(dbContext, DefaultTextService(TwoPostsJson));

        var run = await orchestrator.StartAsync(world.Tenant.Id, world.Brand.Id, world.Campaign.Id, world.UserId, CancellationToken.None);
        await orchestrator.AdvanceAsync(run, TenantMemberRole.Owner, CancellationToken.None);

        var campaign = await dbContext.MarketingCampaigns.FirstAsync(c => c.Id == world.Campaign.Id);

        Assert.NotNull(campaign.CompetitorResearchJson);
        Assert.Contains("\"c\"", campaign.CompetitorResearchJson);

        Assert.NotNull(campaign.DiagnosisJson);
        var diagnosis = System.Text.Json.JsonDocument.Parse(campaign.DiagnosisJson).RootElement;
        Assert.Equal("ملخص", diagnosis.GetProperty("businessSummary").GetString());

        Assert.NotNull(campaign.AiPlanJson);
        Assert.Contains("executiveSummary", campaign.AiPlanJson); // assembled from the three strategy sub-artifacts
        Assert.NotNull(campaign.AiGeneratedAt);
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

    // ── EnsureContentBatchAsync (generate-content's "generate another batch") ─────────────

    [Fact]
    public async Task EnsureContentBatchAsync_LetsAnApprovedRun_GenerateASecondBatch_AndChargesCoinsAgain()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext, coinBalance: 100_000, freeMarketingPlanUsed: false);
        var orchestrator = world.BuildOrchestrator(dbContext, DefaultTextService(TwoPostsJson));

        var run = await orchestrator.StartAsync(world.Tenant.Id, world.Brand.Id, world.Campaign.Id, world.UserId, CancellationToken.None);
        await orchestrator.AdvanceAsync(run, TenantMemberRole.Owner, CancellationToken.None);
        var campaign = await dbContext.MarketingCampaigns.FirstAsync(c => c.Id == world.Campaign.Id);
        await new PipelineApprovalService(dbContext).ApproveAsync(run, campaign, CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        await orchestrator.AdvanceAsync(run, TenantMemberRole.Owner, CancellationToken.None); // first batch

        Assert.Equal(2, await dbContext.ContentItems.CountAsync(c => c.CampaignId == world.Campaign.Id));
        var stageAfterFirst = await dbContext.AiPipelineStages
            .SingleAsync(s => s.RunId == run.Id && s.Kind == AiPipelineStageKind.ContentPlan);
        Assert.Equal(AiPipelineStageStatus.Completed, stageAfterFirst.Status);
        Assert.True(stageAfterFirst.CoinsCharged > 0);

        // What the generate-content shim does for a repeat call: reset the same row rather than
        // insert a second one (the unique index wouldn't allow that anyway), with CoinsCharged
        // zeroed — a second batch is its own billable action, not a free re-run.
        var stage = await orchestrator.EnsureContentBatchAsync(run, CancellationToken.None);
        Assert.Equal(AiPipelineStageStatus.Pending, stage.Status);
        Assert.Equal(0, stage.CoinsCharged);

        await orchestrator.AdvanceAsync(run, TenantMemberRole.Owner, CancellationToken.None);

        Assert.Equal(4, await dbContext.ContentItems.CountAsync(c => c.CampaignId == world.Campaign.Id)); // added, not replaced

        var reloadedRun = await dbContext.AiPipelineRuns.SingleAsync(r => r.Id == run.Id);
        // 8,500 for the full first run (see the full-run test's breakdown) + another 1,000 for the
        // second content batch — content generation is charged per batch, not once per run.
        Assert.Equal(9_500, reloadedRun.TotalCoinsSpent);
    }

    [Fact]
    public async Task EnsureContentBatchAsync_CreatesTheStage_WhenTheRunNeverHadOne()
    {
        // The shape of every C18-backfilled run: HumanApproval Completed, no ContentPlan row at all,
        // since the backfill migration only reconstructs the strategy half of the graph.
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext, coinBalance: 100_000, freeMarketingPlanUsed: false);
        var orchestrator = world.BuildOrchestrator(dbContext, DefaultTextService(TwoPostsJson));
        var run = new AiPipelineRun
        {
            Id = Guid.NewGuid(), TenantId = world.Tenant.Id, BrandProfileId = world.Brand.Id, CampaignId = world.Campaign.Id,
            TriggeredBy = world.UserId, Status = AiPipelineRunStatus.Completed, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        dbContext.AiPipelineRuns.Add(run);
        dbContext.AiPipelineStages.Add(new AiPipelineStage
        {
            Id = Guid.NewGuid(), RunId = run.Id, Kind = AiPipelineStageKind.HumanApproval,
            Status = AiPipelineStageStatus.Completed, MaxAttempts = 1, CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var stage = await orchestrator.EnsureContentBatchAsync(run, CancellationToken.None);

        Assert.Equal(AiPipelineStageKind.ContentPlan, stage.Kind);
        Assert.Equal(AiPipelineStageStatus.Pending, stage.Status);
        var persisted = await dbContext.AiPipelineStages.SingleAsync(s => s.Id == stage.Id);
        Assert.Equal(AiPipelineStageStatus.Pending, persisted.Status);
    }

    [Fact]
    public async Task EnsureContentBatchAsync_LeavesARunningStageAlone()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext, coinBalance: 100_000, freeMarketingPlanUsed: false);
        var orchestrator = world.BuildOrchestrator(dbContext, DefaultTextService(TwoPostsJson));
        var run = new AiPipelineRun
        {
            Id = Guid.NewGuid(), TenantId = world.Tenant.Id, BrandProfileId = world.Brand.Id, CampaignId = world.Campaign.Id,
            TriggeredBy = world.UserId, Status = AiPipelineRunStatus.Running, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        dbContext.AiPipelineRuns.Add(run);
        var running = new AiPipelineStage
        {
            Id = Guid.NewGuid(), RunId = run.Id, Kind = AiPipelineStageKind.ContentPlan,
            Status = AiPipelineStageStatus.Running, LeaseOwner = "some-other-worker", MaxAttempts = 3, CreatedAt = DateTime.UtcNow
        };
        dbContext.AiPipelineStages.Add(running);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var stage = await orchestrator.EnsureContentBatchAsync(run, CancellationToken.None);

        Assert.Equal(AiPipelineStageStatus.Running, stage.Status);
        Assert.Equal("some-other-worker", stage.LeaseOwner);
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

    // ── Claiming and provider throttling ───────────────────────────────────────────────────

    [Fact]
    public async Task AStageAlreadyClaimedByAnotherCaller_IsLeftAlone()
    {
        // Simulates the moment a real race would matter: this stage looks Pending to nobody, because
        // something else already has it. GetRunnableStages only ever selects Pending stages, so a
        // Running one — however it got that way — is simply never handed to an executor again.
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext, coinBalance: 100_000, freeMarketingPlanUsed: false);
        var orchestrator = world.BuildOrchestrator(dbContext, DefaultTextService(TwoPostsJson));

        var run = await orchestrator.StartAsync(world.Tenant.Id, world.Brand.Id, world.Campaign.Id, world.UserId, CancellationToken.None);

        var campaignAnalysis = await dbContext.AiPipelineStages
            .SingleAsync(s => s.RunId == run.Id && s.Kind == AiPipelineStageKind.CampaignAnalysis);
        campaignAnalysis.Status = AiPipelineStageStatus.Running;
        campaignAnalysis.LeaseOwner = "some-other-worker";
        campaignAnalysis.LeaseExpiresAt = DateTime.UtcNow.AddMinutes(5);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        await orchestrator.AdvanceAsync(run, TenantMemberRole.Owner, CancellationToken.None);

        campaignAnalysis = await dbContext.AiPipelineStages.SingleAsync(s => s.Id == campaignAnalysis.Id);
        Assert.Equal(AiPipelineStageStatus.Running, campaignAnalysis.Status);
        Assert.Equal("some-other-worker", campaignAnalysis.LeaseOwner);

        // Everything that doesn't depend on it still proceeds — one stage being claimed elsewhere
        // doesn't stall the rest of the run.
        var brandAnalysis = await dbContext.AiPipelineStages
            .SingleAsync(s => s.RunId == run.Id && s.Kind == AiPipelineStageKind.BrandAnalysis);
        Assert.Equal(AiPipelineStageStatus.Completed, brandAnalysis.Status);
    }

    [Fact]
    public async Task EveryProviderCall_IsMadeUnderTheConcurrencyLimiter_ExceptThePureStages()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext, coinBalance: 100_000, freeMarketingPlanUsed: false);

        var limiter = Substitute.For<IAiProviderConcurrencyLimiter>();
        limiter.AcquireAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDisposable>(Substitute.For<IDisposable>()));

        var orchestrator = world.BuildOrchestrator(dbContext, DefaultTextService(TwoPostsJson), limiter: limiter);

        var run = await orchestrator.StartAsync(world.Tenant.Id, world.Brand.Id, world.Campaign.Id, world.UserId, CancellationToken.None);
        await orchestrator.AdvanceAsync(run, TenantMemberRole.Owner, CancellationToken.None);

        // One AdvanceAsync call reaches HumanApproval and parks there, so only the stages up to and
        // including StrategyAssemble have run: BrandAnalysis, CampaignAnalysis, StrategyPositioning,
        // StrategyBlueprint and StrategyRoadmap are Groq (5); MarketResearch and CompetitorResearch
        // are Tavily (2). StrategyAssemble is pure composition and HumanApproval waits on a person —
        // neither calls a provider, so neither should ever acquire anything, and ContentPlan/
        // ContentImage haven't run yet at all.
        await limiter.Received(5).AcquireAsync("Groq", Arg.Any<CancellationToken>());
        await limiter.Received(2).AcquireAsync("Tavily", Arg.Any<CancellationToken>());
        await limiter.DidNotReceive().AcquireAsync("HuggingFace", Arg.Any<CancellationToken>());
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
            AppDbContext dbContext, FakeTextGenerationService text, FakeImageService? image = null,
            IAiProviderConcurrencyLimiter? limiter = null)
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

            return new PipelineOrchestrator(dbContext, artifacts, Costs, limiter ?? new AiProviderConcurrencyLimiter(), executors);
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
