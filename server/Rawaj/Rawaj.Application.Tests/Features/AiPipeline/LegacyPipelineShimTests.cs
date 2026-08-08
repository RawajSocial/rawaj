using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.AiPipeline.Services;
using Rawaj.Application.Features.Campaigns.GenerateBusinessDiagnosis;
using Rawaj.Application.Features.Campaigns.GenerateCampaignContent;
using Rawaj.Application.Features.Campaigns.GenerateMarketingPlan;
using Rawaj.Application.Features.Campaigns.RefineCampaignPlan;
using Rawaj.Application.Features.Campaigns.ResearchCampaignCompetitors;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Persistence;
using Xunit;

namespace Rawaj.Application.Tests.Features.AiPipeline;

/// <summary>
/// C19 cutover: the four legacy campaign endpoints (research-competitors, diagnose-business,
/// generate-plan, refine-plan) that became thin shims onto the pipeline orchestrator. The
/// orchestrator itself is substituted — its graph-walking is PipelineOrchestratorTests' job, and its
/// write-through is FullRun_WritesThroughToTheLegacyCampaignColumns' — what's tested here is each
/// shim's own logic: which run it reuses versus starts fresh, and how it turns "the column is still
/// null" into the same kind of message the old handler would have returned directly.
/// </summary>
public class LegacyPipelineShimTests
{
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

    // ── ResearchCampaignCompetitors ────────────────────────────────────────────────────────

    [Fact]
    public async Task ResearchCompetitors_StartsAFreshRun_WhenTheCampaignHasNone()
    {
        var world = await SeedAsync(coinBalance: 100_000);
        var orchestrator = Substitute.For<IPipelineOrchestrator>();
        var run = NewRun(world);
        orchestrator.StartAsync(world.Tenant.Id, world.Brand.Id, world.Campaign.Id, world.UserId, Arg.Any<CancellationToken>())
            .Returns(run);
        orchestrator.When(o => o.AdvanceAsync(Arg.Any<AiPipelineRun>(), Arg.Any<TenantMemberRole>(), Arg.Any<CancellationToken>()))
            .Do(_ => world.Campaign.CompetitorResearchJson = """{"summary":"s","competitors":[],"sources":[],"unavailable":false,"note":null}""");

        var handler = new ResearchCampaignCompetitorsCommandHandler(
            world.DbContext, world.CurrentUserService(), world.CurrentTenantContext(), orchestrator);

        var result = await handler.Handle(new ResearchCampaignCompetitorsCommand(world.Campaign.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.True(result.Data!.Succeeded);
        var campaign = await world.DbContext.MarketingCampaigns.SingleAsync(c => c.Id == world.Campaign.Id);
        Assert.Equal(run.Id, campaign.CurrentPipelineRunId);
    }

    [Fact]
    public async Task ResearchCompetitors_ReusesTheExistingRun_WithoutStartingANewOne()
    {
        var world = await SeedAsync(coinBalance: 100_000);
        var run = NewRun(world);
        world.DbContext.AiPipelineRuns.Add(run);
        world.Campaign.CurrentPipelineRunId = run.Id;
        await world.DbContext.SaveChangesAsync(CancellationToken.None);

        var orchestrator = Substitute.For<IPipelineOrchestrator>();
        orchestrator.When(o => o.AdvanceAsync(Arg.Any<AiPipelineRun>(), Arg.Any<TenantMemberRole>(), Arg.Any<CancellationToken>()))
            .Do(_ => world.Campaign.CompetitorResearchJson = """{"summary":null,"competitors":[],"sources":[],"unavailable":true,"note":"none"}""");

        var handler = new ResearchCampaignCompetitorsCommandHandler(
            world.DbContext, world.CurrentUserService(), world.CurrentTenantContext(), orchestrator);

        var result = await handler.Handle(new ResearchCampaignCompetitorsCommand(world.Campaign.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.False(result.Data!.Succeeded); // unavailable
        Assert.Equal("none", result.Data.Note);
        await orchestrator.DidNotReceive().StartAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResearchCompetitors_SurfacesTheBlockingCoinsMessage_WhenTheColumnNeverGetsWritten()
    {
        var world = await SeedAsync(coinBalance: 100_000);
        var run = NewRun(world);
        world.DbContext.AiPipelineRuns.Add(run);
        world.Campaign.CurrentPipelineRunId = run.Id;
        var blockedStage = new AiPipelineStage
        {
            Id = Guid.NewGuid(), RunId = run.Id, Kind = AiPipelineStageKind.CampaignAnalysis,
            Status = AiPipelineStageStatus.Pending, LastErrorKind = AiFailureKind.Coins,
            LastError = "Not enough coins.", MaxAttempts = 3, CreatedAt = DateTime.UtcNow
        };
        world.DbContext.AiPipelineStages.Add(blockedStage);
        await world.DbContext.SaveChangesAsync(CancellationToken.None);

        var orchestrator = Substitute.For<IPipelineOrchestrator>(); // AdvanceAsync is a no-op — nothing gets written

        var handler = new ResearchCampaignCompetitorsCommandHandler(
            world.DbContext, world.CurrentUserService(), world.CurrentTenantContext(), orchestrator);

        var result = await handler.Handle(new ResearchCampaignCompetitorsCommand(world.Campaign.Id), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Not enough coins.", result.ErrorMessage);
    }

    // ── GenerateBusinessDiagnosis ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DiagnoseBusiness_ReturnsTheColumn_OnceTheRunWritesIt()
    {
        var world = await SeedAsync(coinBalance: 100_000);
        var run = NewRun(world);
        orchestratorReturning(run, () => world.Campaign.DiagnosisJson = """{"businessSummary":"s"}""", out var orchestrator);

        var handler = new GenerateBusinessDiagnosisCommandHandler(
            world.DbContext, world.CurrentUserService(), world.CurrentTenantContext(), orchestrator);

        var result = await handler.Handle(new GenerateBusinessDiagnosisCommand(world.Campaign.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Contains("businessSummary", result.Data!.DiagnosisJson);
    }

    [Fact]
    public async Task DiagnoseBusiness_Fails_WhenTheCampaignDoesNotExist()
    {
        var world = await SeedAsync(coinBalance: 100_000);
        var orchestrator = Substitute.For<IPipelineOrchestrator>();
        var handler = new GenerateBusinessDiagnosisCommandHandler(
            world.DbContext, world.CurrentUserService(), world.CurrentTenantContext(), orchestrator);

        var result = await handler.Handle(new GenerateBusinessDiagnosisCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Campaign not found.", result.ErrorMessage);
    }

    // ── GenerateMarketingPlan ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GeneratePlan_ReturnsTheAssembledStrategy()
    {
        var world = await SeedAsync(coinBalance: 100_000);
        var run = NewRun(world);
        orchestratorReturning(run, () =>
        {
            world.Campaign.AiPlanJson = """{"executiveSummary":"s"}""";
            world.Campaign.AiGeneratedAt = DateTime.UtcNow;
        }, out var orchestrator);

        var handler = new GenerateMarketingPlanCommandHandler(
            world.DbContext, world.CurrentUserService(), world.CurrentTenantContext(), orchestrator);

        var result = await handler.Handle(new GenerateMarketingPlanCommand(world.Campaign.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Contains("executiveSummary", result.Data!.AiPlanJson);
    }

    // ── RefineCampaignPlan ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RefinePlan_Fails_WithoutCallingTheService_WhenCoinsAreShort()
    {
        var world = await SeedAsync(coinBalance: 100);
        var refinementService = Substitute.For<IPipelineStrategyRefinementService>();
        var handler = new RefineCampaignPlanCommandHandler(
            world.DbContext, world.CurrentUserService(), world.CurrentTenantContext(), refinementService, new TestCoinCosts());

        var result = await handler.Handle(new RefineCampaignPlanCommand(world.Campaign.Id, "make it punchier"), CancellationToken.None);

        Assert.False(result.Succeeded);
        await refinementService.DidNotReceive().RefineAsync(
            Arg.Any<TenantBrandProfile>(), Arg.Any<MarketingCampaign>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefinePlan_ChargesCoins_OnlyOnSuccess_AndReturnsTheRefinedPlan()
    {
        var world = await SeedAsync(coinBalance: 100_000);
        var refinementService = Substitute.For<IPipelineStrategyRefinementService>();
        refinementService.RefineAsync(
                Arg.Any<TenantBrandProfile>(), Arg.Any<MarketingCampaign>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                world.Campaign.AiPlanJson = """{"executiveSummary":"refined"}""";
                world.Campaign.AiGeneratedAt = DateTime.UtcNow;
                return Result<AiArtifact>.Success(new AiArtifact
                {
                    Id = Guid.NewGuid(), TenantId = world.Tenant.Id, BrandProfileId = world.Brand.Id, CampaignId = world.Campaign.Id,
                    Kind = AiArtifactKind.Strategy, Version = 2, IsCurrent = true, ContentJson = world.Campaign.AiPlanJson,
                    SchemaVersion = 1, CreatedAt = DateTime.UtcNow
                });
            });

        var handler = new RefineCampaignPlanCommandHandler(
            world.DbContext, world.CurrentUserService(), world.CurrentTenantContext(), refinementService, new TestCoinCosts());

        var result = await handler.Handle(new RefineCampaignPlanCommand(world.Campaign.Id, "shorter"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Contains("refined", result.Data!.AiPlanJson);
        var balance = await world.DbContext.Tenants.Where(t => t.Id == world.Tenant.Id).Select(t => t.CoinBalance).FirstAsync();
        Assert.Equal(100_000 - 2_500, balance);
    }

    // ── GenerateCampaignContent ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateContent_Fails_WhenTheStrategyIsNotApproved()
    {
        var world = await SeedAsync(coinBalance: 100_000);
        var orchestrator = Substitute.For<IPipelineOrchestrator>();
        var handler = new GenerateCampaignContentCommandHandler(
            world.DbContext, world.CurrentUserService(), world.CurrentTenantContext(), orchestrator);

        var result = await handler.Handle(
            new GenerateCampaignContentCommand(world.Campaign.Id, 2, Language.Ar), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("Approve the campaign strategy", result.ErrorMessage);
        await orchestrator.DidNotReceive().EnsureContentBatchAsync(Arg.Any<AiPipelineRun>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateContent_ReportsOnlyTheItemsCreatedByThisCall_NotThePreExistingOnes()
    {
        var world = await SeedAsync(coinBalance: 100_000);
        world.Campaign.PlanApprovedAt = DateTime.UtcNow;
        var run = NewRun(world);
        world.DbContext.AiPipelineRuns.Add(run);
        world.Campaign.CurrentPipelineRunId = run.Id;

        // A post from an earlier batch — must not be counted as part of this call's result.
        world.DbContext.ContentItems.Add(NewContentItem(world));
        await world.DbContext.SaveChangesAsync(CancellationToken.None);

        var stage = new AiPipelineStage
        {
            Id = Guid.NewGuid(), RunId = run.Id, Kind = AiPipelineStageKind.ContentPlan,
            Status = AiPipelineStageStatus.Pending, MaxAttempts = 3, CreatedAt = DateTime.UtcNow
        };
        var orchestrator = Substitute.For<IPipelineOrchestrator>();
        orchestrator.EnsureContentBatchAsync(run, Arg.Any<CancellationToken>()).Returns(stage);
        orchestrator.When(o => o.AdvanceAsync(run, Arg.Any<TenantMemberRole>(), Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                world.DbContext.ContentItems.Add(NewContentItem(world));
                world.DbContext.ContentItems.Add(NewContentItem(world));
                world.DbContext.SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();
            });

        var handler = new GenerateCampaignContentCommandHandler(
            world.DbContext, world.CurrentUserService(), world.CurrentTenantContext(), orchestrator);

        var result = await handler.Handle(
            new GenerateCampaignContentCommand(world.Campaign.Id, 2, Language.Ar), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Data!.GeneratedCount); // not 3 — the pre-existing post doesn't count

        var reloadedRun = await world.DbContext.AiPipelineRuns.SingleAsync(r => r.Id == run.Id);
        Assert.Equal(2, reloadedRun.ContentPostCount);
        Assert.Equal(Language.Ar, reloadedRun.ContentLanguage);
    }

    [Fact]
    public async Task GenerateContent_Fails_WhenNothingNewWasCreated()
    {
        var world = await SeedAsync(coinBalance: 100_000);
        world.Campaign.PlanApprovedAt = DateTime.UtcNow;
        var run = NewRun(world);
        world.DbContext.AiPipelineRuns.Add(run);
        world.Campaign.CurrentPipelineRunId = run.Id;
        var stage = new AiPipelineStage
        {
            Id = Guid.NewGuid(), RunId = run.Id, Kind = AiPipelineStageKind.ContentPlan,
            Status = AiPipelineStageStatus.Pending, LastError = "The AI provider did not return a response.",
            MaxAttempts = 3, CreatedAt = DateTime.UtcNow
        };
        world.DbContext.AiPipelineStages.Add(stage);
        await world.DbContext.SaveChangesAsync(CancellationToken.None);

        var orchestrator = Substitute.For<IPipelineOrchestrator>();
        orchestrator.EnsureContentBatchAsync(run, Arg.Any<CancellationToken>()).Returns(stage);
        // AdvanceAsync is a no-op — nothing gets created.

        var handler = new GenerateCampaignContentCommandHandler(
            world.DbContext, world.CurrentUserService(), world.CurrentTenantContext(), orchestrator);

        var result = await handler.Handle(
            new GenerateCampaignContentCommand(world.Campaign.Id, 2, Language.Ar), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("The AI provider did not return a response.", result.ErrorMessage);
    }

    private static ContentItem NewContentItem(World world) => new()
    {
        Id = Guid.NewGuid(), CampaignId = world.Campaign.Id, TenantId = world.Tenant.Id, BrandProfileId = world.Brand.Id,
        CreatedBy = world.UserId, ContentType = ContentType.Post, Platform = SocialPlatform.Instagram, Language = Language.Ar,
        Content = "post", Status = ContentStatus.Draft, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
    };

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────

    private static AiPipelineRun NewRun(World world) => new()
    {
        Id = Guid.NewGuid(), TenantId = world.Tenant.Id, BrandProfileId = world.Brand.Id, CampaignId = world.Campaign.Id,
        TriggeredBy = world.UserId, Status = AiPipelineRunStatus.Running, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
    };

    private static void orchestratorReturning(AiPipelineRun run, Action onAdvance, out IPipelineOrchestrator orchestrator)
    {
        var sub = Substitute.For<IPipelineOrchestrator>();
        sub.StartAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(run);
        sub.When(o => o.AdvanceAsync(Arg.Any<AiPipelineRun>(), Arg.Any<TenantMemberRole>(), Arg.Any<CancellationToken>()))
            .Do(_ => onAdvance());
        orchestrator = sub;
    }

    private sealed record World(AppDbContext DbContext, Tenant Tenant, TenantBrandProfile Brand, MarketingCampaign Campaign, Guid UserId)
    {
        public ICurrentUserService CurrentUserService()
        {
            var service = Substitute.For<ICurrentUserService>();
            service.UserId.Returns(UserId);
            return service;
        }

        public ICurrentTenantContext CurrentTenantContext()
        {
            var context = Substitute.For<ICurrentTenantContext>();
            context.TenantId.Returns(Tenant.Id);
            context.Role.Returns(TenantMemberRole.Owner);
            return context;
        }
    }

    private static async Task<World> SeedAsync(int coinBalance)
    {
        var dbContext = TestDbContextFactory.Create();
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

        var userId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(), Name = "Test Tenant", Subdomain = Guid.NewGuid().ToString("N"),
            TenantType = TenantType.Business, OwnerUserId = userId, SubscriptionId = subscriptionId,
            CoinBalance = coinBalance, IsActive = true, IsActivated = true, CreatedAt = now, UpdatedAt = now
        };
        dbContext.Tenants.Add(tenant);

        var brand = new TenantBrandProfile
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, Name = "Qahwa",
            Status = BrandProfileStatus.Active, CreatedAt = now, UpdatedAt = now
        };
        dbContext.TenantBrandProfiles.Add(brand);

        var campaign = new MarketingCampaign
        {
            Id = Guid.NewGuid(), BrandProfileId = brand.Id, CreatedBy = Guid.NewGuid(),
            Name = "Summer Push", TargetPlatforms = ["Instagram"],
            Status = CampaignStatus.Draft, CreatedAt = now, UpdatedAt = now
        };
        dbContext.MarketingCampaigns.Add(campaign);

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return new World(dbContext, tenant, brand, campaign, userId);
    }
}
