using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Services;
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
/// Approval and refinement — the two things that happen to a strategy after it exists, neither of
/// which is a graph node. HumanApproval is RequiresHuman so nothing ever dispatches it to a worker;
/// refinement has no stage at all, since the graph has no node for "user typed feedback into a box".
/// </summary>
public class ApprovalAndRefinementServiceTests
{
    private const string StrategyV1 = """
        {"executiveSummary":"v1","businessAndMarketAnalysis":"a","brandStrategy":"b","marketingStrategy":"c",
         "campaignBlueprint":{"pillars":[]},"contentProductionPlan":"d","executionRoadmap":"e","aiRecommendations":[]}
        """;

    private const string StrategyRefined = """
        {"executiveSummary":"refined","businessAndMarketAnalysis":"a","brandStrategy":"b","marketingStrategy":"c",
         "campaignBlueprint":{"pillars":[]},"contentProductionPlan":"d","executionRoadmap":"e","aiRecommendations":[]}
        """;

    // ── Approval ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Approve_StampsTheExactArtifactVersion_AndCompletesTheApprovalStage()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var strategy = await world.AddStrategyVersionAsync(dbContext, StrategyV1);
        var stage = world.AddStage(dbContext, AiPipelineStageKind.HumanApproval, AiPipelineStageStatus.AwaitingApproval);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var service = new PipelineApprovalService(dbContext);
        var result = await service.ApproveAsync(world.Run, world.Campaign, CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(strategy.Id, result.Data!.Id);
        Assert.Equal(strategy.Id, world.Campaign.ApprovedStrategyArtifactId);
        Assert.NotNull(world.Campaign.PlanApprovedAt);

        var reloaded = await dbContext.AiPipelineStages.SingleAsync(s => s.Id == stage.Id);
        Assert.Equal(AiPipelineStageStatus.Completed, reloaded.Status);
        Assert.Equal(strategy.Id, reloaded.ArtifactId);
    }

    [Fact]
    public async Task Approve_ActivatesADraftCampaign()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        await world.AddStrategyVersionAsync(dbContext, StrategyV1);
        world.AddStage(dbContext, AiPipelineStageKind.HumanApproval, AiPipelineStageStatus.AwaitingApproval);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var service = new PipelineApprovalService(dbContext);
        await service.ApproveAsync(world.Run, world.Campaign, CancellationToken.None);

        Assert.Equal(CampaignStatus.Active, world.Campaign.Status);
    }

    [Fact]
    public async Task Approve_Fails_WhenNothingIsAwaitingApproval()
    {
        // No HumanApproval stage at all — the run hasn't reached it yet.
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var service = new PipelineApprovalService(dbContext);

        var result = await service.ApproveAsync(world.Run, world.Campaign, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Null(world.Campaign.ApprovedStrategyArtifactId);
    }

    [Fact]
    public async Task Approve_Fails_WhenThereIsNoCurrentStrategy()
    {
        // Unreachable via the graph (StrategyAssemble must complete first), but the stage row could
        // still exist in a test double or a data-repair scenario — fail loudly rather than approving
        // nothing.
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        world.AddStage(dbContext, AiPipelineStageKind.HumanApproval, AiPipelineStageStatus.AwaitingApproval);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var service = new PipelineApprovalService(dbContext);
        var result = await service.ApproveAsync(world.Run, world.Campaign, CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    // ── Refinement ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Refine_WritesANewVersion_WithoutDeletingTheOne_ItReplaces()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var v1 = await world.AddStrategyVersionAsync(dbContext, StrategyV1);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var artifacts = new PipelineArtifactStore(dbContext);
        var textService = TextServiceReturning(StrategyRefined);
        var service = new PipelineStrategyRefinementService(dbContext, textService, artifacts);

        var result = await service.RefineAsync(world.Brand, world.Campaign, world.Run.TriggeredBy, "make it punchier", CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Data!.Version);
        Assert.Contains("refined", result.Data.ContentJson);

        var original = await dbContext.AiArtifacts.SingleAsync(a => a.Id == v1.Id);
        Assert.False(original.IsCurrent);
        Assert.Contains("v1", original.ContentJson);
    }

    [Fact]
    public async Task Refine_WritesThroughToAiPlanJson()
    {
        // Phase 6 compatibility: the legacy strategy review UI reads campaign.AiPlanJson directly, not
        // the artifact — a refinement that only wrote a new artifact version would be invisible to it.
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        await world.AddStrategyVersionAsync(dbContext, StrategyV1);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var artifacts = new PipelineArtifactStore(dbContext);
        var service = new PipelineStrategyRefinementService(dbContext, TextServiceReturning(StrategyRefined), artifacts);

        var result = await service.RefineAsync(world.Brand, world.Campaign, world.Run.TriggeredBy, "make it punchier", CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Contains("refined", world.Campaign.AiPlanJson);
        Assert.NotNull(world.Campaign.AiGeneratedAt);
    }

    [Fact]
    public async Task Refine_LogsTheProviderCall_WithNoBackingStage()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        await world.AddStrategyVersionAsync(dbContext, StrategyV1);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var artifacts = new PipelineArtifactStore(dbContext);
        var service = new PipelineStrategyRefinementService(dbContext, TextServiceReturning(StrategyRefined), artifacts);

        await service.RefineAsync(world.Brand, world.Campaign, world.Run.TriggeredBy, "shorter", CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var job = await dbContext.AiJobs.SingleAsync();
        Assert.Null(job.PipelineStageId);
        Assert.Equal(world.Tenant.Id, job.TenantId);
    }

    [Fact]
    public async Task Refine_Fails_OnceTheStrategyIsApproved()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        await world.AddStrategyVersionAsync(dbContext, StrategyV1);
        world.Campaign.PlanApprovedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var artifacts = new PipelineArtifactStore(dbContext);
        var service = new PipelineStrategyRefinementService(dbContext, TextServiceReturning(StrategyRefined), artifacts);

        var result = await service.RefineAsync(world.Brand, world.Campaign, world.Run.TriggeredBy, "change it", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("already approved", result.ErrorMessage);
    }

    [Fact]
    public async Task Refine_Fails_WhenThereIsNoStrategyYet()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var artifacts = new PipelineArtifactStore(dbContext);
        var service = new PipelineStrategyRefinementService(dbContext, TextServiceReturning(StrategyRefined), artifacts);

        var result = await service.RefineAsync(world.Brand, world.Campaign, world.Run.TriggeredBy, "change it", CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Refine_LeavesThePreviousVersionCurrent_WhenTheModelReturnsUnreadableJson()
    {
        // Especially important here: this is about to replace an already-paid-for strategy.
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var v1 = await world.AddStrategyVersionAsync(dbContext, StrategyV1);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var artifacts = new PipelineArtifactStore(dbContext);
        var service = new PipelineStrategyRefinementService(dbContext, TextServiceReturning("not json at all"), artifacts);

        var result = await service.RefineAsync(world.Brand, world.Campaign, world.Run.TriggeredBy, "change it", CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        Assert.False(result.Succeeded);
        var reloaded = await dbContext.AiArtifacts.SingleAsync(a => a.Id == v1.Id);
        Assert.True(reloaded.IsCurrent);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────

    private static IAiTextGenerationService TextServiceReturning(string text)
    {
        var service = Substitute.For<IAiTextGenerationService>();
        service.GenerateTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<AiTextGenerationOptions?>())
            .Returns(AiTextGenerationResult.Success(text, 500, "test-model", 1234));
        return service;
    }

    private sealed record World(Tenant Tenant, TenantBrandProfile Brand, MarketingCampaign Campaign, AiPipelineRun Run)
    {
        public AiPipelineStage AddStage(AppDbContext dbContext, AiPipelineStageKind kind, AiPipelineStageStatus status)
        {
            var stage = new AiPipelineStage
            {
                Id = Guid.NewGuid(),
                RunId = Run.Id,
                Kind = kind,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };
            dbContext.AiPipelineStages.Add(stage);
            return stage;
        }

        public async Task<AiArtifact> AddStrategyVersionAsync(AppDbContext dbContext, string contentJson)
        {
            var store = new PipelineArtifactStore(dbContext);
            return await store.AddVersionAsync(
                stage: null, Tenant.Id, Brand.Id, Campaign.Id, AiArtifactKind.Strategy, contentJson, null, CancellationToken.None);
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
