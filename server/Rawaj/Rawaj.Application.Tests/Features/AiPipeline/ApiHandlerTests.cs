using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Services;
using Rawaj.Application.Features.AiPipeline.ApproveStrategy;
using Rawaj.Application.Features.AiPipeline.CancelRun;
using Rawaj.Application.Features.AiPipeline.GetArtifact;
using Rawaj.Application.Features.AiPipeline.GetRunStatus;
using Rawaj.Application.Features.AiPipeline.RefineStrategy;
using Rawaj.Application.Features.AiPipeline.ResumeRun;
using Rawaj.Application.Features.AiPipeline.RunStage;
using Rawaj.Application.Features.AiPipeline.Services;
using Rawaj.Application.Features.AiPipeline.StartRun;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Persistence;
using Xunit;

namespace Rawaj.Application.Tests.Features.AiPipeline;

/// <summary>
/// The API layer's own handlers — not the orchestrator's graph-walking (covered by
/// PipelineOrchestratorTests), but the guards each handler adds on top of it: one active run per
/// campaign, which stage statuses a manual retry is allowed from, which run statuses resume/cancel
/// refuse, and that refinement checks coins before calling the refinement service. The orchestrator
/// and the approval/refinement services are substituted here — their own behaviour is tested where
/// they're defined.
/// </summary>
public class ApiHandlerTests
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

    // ── StartRun ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task StartRun_CreatesTheRun_AndPointsTheCampaignAtIt()
    {
        var world = await SeedAsync(coinBalance: 100_000);
        var orchestrator = Substitute.For<IPipelineOrchestrator>();
        var newRun = new AiPipelineRun
        {
            Id = Guid.NewGuid(), TenantId = world.Tenant.Id, BrandProfileId = world.Brand.Id, CampaignId = world.Campaign.Id,
            TriggeredBy = world.UserId, Status = AiPipelineRunStatus.Pending, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        orchestrator.StartAsync(world.Tenant.Id, world.Brand.Id, world.Campaign.Id, world.UserId, Arg.Any<CancellationToken>())
            .Returns(newRun);

        var handler = new StartRunCommandHandler(
            world.DbContext, world.CurrentUserService(), world.CurrentTenantContext(), orchestrator);

        var result = await handler.Handle(new StartRunCommand(world.Campaign.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(newRun.Id, result.Data!.RunId);

        var campaign = await world.DbContext.MarketingCampaigns.SingleAsync(c => c.Id == world.Campaign.Id);
        Assert.Equal(newRun.Id, campaign.CurrentPipelineRunId);
    }

    [Fact]
    public async Task StartRun_Fails_WhenARunIsAlreadyInProgress()
    {
        // Two active runs racing for the same artifacts and the same coin charges is exactly the
        // failure mode a single-run-per-campaign guard exists to prevent.
        var world = await SeedAsync(coinBalance: 100_000);
        world.DbContext.AiPipelineRuns.Add(new AiPipelineRun
        {
            Id = Guid.NewGuid(), TenantId = world.Tenant.Id, BrandProfileId = world.Brand.Id, CampaignId = world.Campaign.Id,
            TriggeredBy = world.UserId, Status = AiPipelineRunStatus.Running, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await world.DbContext.SaveChangesAsync(CancellationToken.None);

        var orchestrator = Substitute.For<IPipelineOrchestrator>();
        var handler = new StartRunCommandHandler(
            world.DbContext, world.CurrentUserService(), world.CurrentTenantContext(), orchestrator);

        var result = await handler.Handle(new StartRunCommand(world.Campaign.Id), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("already in progress", result.ErrorMessage);
        await orchestrator.DidNotReceive().StartAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartRun_AllowsANewRun_OnceThePreviousOneIsTerminal()
    {
        var world = await SeedAsync(coinBalance: 100_000);
        world.DbContext.AiPipelineRuns.Add(new AiPipelineRun
        {
            Id = Guid.NewGuid(), TenantId = world.Tenant.Id, BrandProfileId = world.Brand.Id, CampaignId = world.Campaign.Id,
            TriggeredBy = world.UserId, Status = AiPipelineRunStatus.Completed, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await world.DbContext.SaveChangesAsync(CancellationToken.None);

        var orchestrator = Substitute.For<IPipelineOrchestrator>();
        orchestrator.StartAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new AiPipelineRun
            {
                Id = Guid.NewGuid(), TenantId = world.Tenant.Id, BrandProfileId = world.Brand.Id, CampaignId = world.Campaign.Id,
                TriggeredBy = world.UserId, Status = AiPipelineRunStatus.Pending, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });

        var handler = new StartRunCommandHandler(
            world.DbContext, world.CurrentUserService(), world.CurrentTenantContext(), orchestrator);

        var result = await handler.Handle(new StartRunCommand(world.Campaign.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
    }

    // ── RunStage ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RunStage_RetriesAFailedStage_WithoutResettingAttempts()
    {
        var world = await SeedAsync(coinBalance: 100_000);
        var run = await world.AddRunAsync(AiPipelineRunStatus.Failed);
        var stage = await world.AddStageAsync(run, AiPipelineStageKind.CompetitorResearch, AiPipelineStageStatus.Failed, attempts: 3);

        var orchestrator = Substitute.For<IPipelineOrchestrator>();
        var handler = new RunStageCommandHandler(world.DbContext, world.CurrentTenantContext(), orchestrator);

        var result = await handler.Handle(new RunStageCommand(run.Id, stage.Kind), CancellationToken.None);

        Assert.True(result.Succeeded);
        var reloaded = await world.DbContext.AiPipelineStages.SingleAsync(s => s.Id == stage.Id);
        Assert.Equal(AiPipelineStageStatus.Pending, reloaded.Status);
        Assert.Null(reloaded.NextAttemptAt);
        Assert.Equal(3, reloaded.Attempts); // one more shot, not a fresh budget
        await orchestrator.Received(1).AdvanceAsync(run, TenantMemberRole.Owner, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(AiPipelineStageStatus.Running)]
    [InlineData(AiPipelineStageStatus.Completed)]
    [InlineData(AiPipelineStageStatus.AwaitingApproval)]
    public async Task RunStage_Refuses_ForAStatusThatIsNotRetryable(AiPipelineStageStatus status)
    {
        var world = await SeedAsync(coinBalance: 100_000);
        var run = await world.AddRunAsync(AiPipelineRunStatus.Running);
        var stage = await world.AddStageAsync(run, AiPipelineStageKind.CampaignAnalysis, status);

        var orchestrator = Substitute.For<IPipelineOrchestrator>();
        var handler = new RunStageCommandHandler(world.DbContext, world.CurrentTenantContext(), orchestrator);

        var result = await handler.Handle(new RunStageCommand(run.Id, stage.Kind), CancellationToken.None);

        Assert.False(result.Succeeded);
        await orchestrator.DidNotReceive().AdvanceAsync(Arg.Any<AiPipelineRun>(), Arg.Any<TenantMemberRole>(), Arg.Any<CancellationToken>());
    }

    // ── ResumeRun ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResumeRun_Refuses_WhenAwaitingApproval()
    {
        var world = await SeedAsync(coinBalance: 100_000);
        var run = await world.AddRunAsync(AiPipelineRunStatus.AwaitingApproval);
        var orchestrator = Substitute.For<IPipelineOrchestrator>();
        var handler = new ResumeRunCommandHandler(world.DbContext, world.CurrentTenantContext(), orchestrator);

        var result = await handler.Handle(new ResumeRunCommand(run.Id), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("Approve the strategy", result.ErrorMessage);
    }

    [Theory]
    [InlineData(AiPipelineRunStatus.Completed)]
    [InlineData(AiPipelineRunStatus.Cancelled)]
    [InlineData(AiPipelineRunStatus.Failed)]
    public async Task ResumeRun_Refuses_ForATerminalRun(AiPipelineRunStatus status)
    {
        var world = await SeedAsync(coinBalance: 100_000);
        var run = await world.AddRunAsync(status);
        var orchestrator = Substitute.For<IPipelineOrchestrator>();
        var handler = new ResumeRunCommandHandler(world.DbContext, world.CurrentTenantContext(), orchestrator);

        var result = await handler.Handle(new ResumeRunCommand(run.Id), CancellationToken.None);

        Assert.False(result.Succeeded);
        await orchestrator.DidNotReceive().AdvanceAsync(Arg.Any<AiPipelineRun>(), Arg.Any<TenantMemberRole>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResumeRun_Advances_WhenAwaitingCoins()
    {
        var world = await SeedAsync(coinBalance: 100_000);
        var run = await world.AddRunAsync(AiPipelineRunStatus.AwaitingCoins);
        var orchestrator = Substitute.For<IPipelineOrchestrator>();
        var handler = new ResumeRunCommandHandler(world.DbContext, world.CurrentTenantContext(), orchestrator);

        var result = await handler.Handle(new ResumeRunCommand(run.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        await orchestrator.Received(1).AdvanceAsync(run, TenantMemberRole.Owner, Arg.Any<CancellationToken>());
    }

    // ── CancelRun ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelRun_Refuses_ForAnAlreadyFinishedRun()
    {
        var world = await SeedAsync(coinBalance: 100_000);
        var run = await world.AddRunAsync(AiPipelineRunStatus.Completed);
        var orchestrator = Substitute.For<IPipelineOrchestrator>();
        var handler = new CancelRunCommandHandler(world.DbContext, world.CurrentTenantContext(), orchestrator);

        var result = await handler.Handle(new CancelRunCommand(run.Id), CancellationToken.None);

        Assert.False(result.Succeeded);
        await orchestrator.DidNotReceive().CancelAsync(Arg.Any<AiPipelineRun>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelRun_CancelsARunningRun()
    {
        var world = await SeedAsync(coinBalance: 100_000);
        var run = await world.AddRunAsync(AiPipelineRunStatus.Running);
        var orchestrator = Substitute.For<IPipelineOrchestrator>();
        var handler = new CancelRunCommandHandler(world.DbContext, world.CurrentTenantContext(), orchestrator);

        var result = await handler.Handle(new CancelRunCommand(run.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        await orchestrator.Received(1).CancelAsync(run, Arg.Any<CancellationToken>());
    }

    // ── ApproveStrategy ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ApproveStrategy_ApprovesThenAdvancesTheRunOnce()
    {
        var world = await SeedAsync(coinBalance: 100_000);
        var run = await world.AddRunAsync(AiPipelineRunStatus.AwaitingApproval);
        await world.AddStageAsync(run, AiPipelineStageKind.HumanApproval, AiPipelineStageStatus.AwaitingApproval);
        var strategy = await world.AddStrategyArtifactAsync();

        var orchestrator = Substitute.For<IPipelineOrchestrator>();
        var handler = new ApproveStrategyCommandHandler(
            world.DbContext, world.CurrentTenantContext(), new PipelineApprovalService(world.DbContext), orchestrator);

        var result = await handler.Handle(new ApproveStrategyCommand(run.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(strategy.Id, result.Data!.ApprovedArtifactId);
        await orchestrator.Received(1).AdvanceAsync(run, TenantMemberRole.Owner, Arg.Any<CancellationToken>());

        var campaign = await world.DbContext.MarketingCampaigns.SingleAsync(c => c.Id == world.Campaign.Id);
        Assert.Equal(strategy.Id, campaign.ApprovedStrategyArtifactId);
    }

    // ── RefineStrategy ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RefineStrategy_Fails_WithoutCallingTheService_WhenCoinsAreShort()
    {
        var world = await SeedAsync(coinBalance: 100);
        var refinementService = Substitute.For<IPipelineStrategyRefinementService>();
        var handler = new RefineStrategyCommandHandler(
            world.DbContext, world.CurrentUserService(), world.CurrentTenantContext(), refinementService, new TestCoinCosts());

        var result = await handler.Handle(new RefineStrategyCommand(world.Campaign.Id, "make it punchier"), CancellationToken.None);

        Assert.False(result.Succeeded);
        await refinementService.DidNotReceive().RefineAsync(
            Arg.Any<TenantBrandProfile>(), Arg.Any<MarketingCampaign>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefineStrategy_ChargesCoins_OnlyOnSuccess()
    {
        var world = await SeedAsync(coinBalance: 100_000);
        var refinementService = Substitute.For<IPipelineStrategyRefinementService>();
        var artifact = new AiArtifact
        {
            Id = Guid.NewGuid(), TenantId = world.Tenant.Id, BrandProfileId = world.Brand.Id, CampaignId = world.Campaign.Id,
            Kind = AiArtifactKind.Strategy, Version = 2, IsCurrent = true, ContentJson = "{}", SchemaVersion = 1, CreatedAt = DateTime.UtcNow
        };
        refinementService.RefineAsync(
                Arg.Any<TenantBrandProfile>(), Arg.Any<MarketingCampaign>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<AiArtifact>.Success(artifact));

        var handler = new RefineStrategyCommandHandler(
            world.DbContext, world.CurrentUserService(), world.CurrentTenantContext(), refinementService, new TestCoinCosts());

        var result = await handler.Handle(new RefineStrategyCommand(world.Campaign.Id, "shorter"), CancellationToken.None);
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Data!.Version);

        var balance = await world.DbContext.Tenants.Where(t => t.Id == world.Tenant.Id).Select(t => t.CoinBalance).FirstAsync();
        Assert.Equal(100_000 - 2_500, balance);
    }

    // ── GetRunStatus / GetArtifact ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRunStatus_ReturnsProgressAndPerStageSummaries()
    {
        var world = await SeedAsync(coinBalance: 100_000);
        var run = await world.AddRunAsync(AiPipelineRunStatus.Running);
        await world.AddStageAsync(run, AiPipelineStageKind.BrandAnalysis, AiPipelineStageStatus.Completed);
        await world.AddStageAsync(run, AiPipelineStageKind.CampaignAnalysis, AiPipelineStageStatus.Running);

        var handler = new GetRunStatusQueryHandler(world.DbContext, world.CurrentTenantContext());
        var result = await handler.Handle(new GetRunStatusQuery(run.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Data!.Stages.Count);
        Assert.Equal(50, result.Data.Progress.Percent);
    }

    [Fact]
    public async Task GetArtifact_Fails_WhenNotYetGenerated()
    {
        var world = await SeedAsync(coinBalance: 100_000);
        var handler = new GetArtifactQueryHandler(
            world.DbContext, world.CurrentTenantContext(), new PipelineArtifactStore(world.DbContext));

        var result = await handler.Handle(new GetArtifactQuery(world.Campaign.Id, AiArtifactKind.Strategy), CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────

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

        public async Task<AiPipelineRun> AddRunAsync(AiPipelineRunStatus status)
        {
            var run = new AiPipelineRun
            {
                Id = Guid.NewGuid(), TenantId = Tenant.Id, BrandProfileId = Brand.Id, CampaignId = Campaign.Id,
                TriggeredBy = UserId, Status = status, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            DbContext.AiPipelineRuns.Add(run);
            await DbContext.SaveChangesAsync(CancellationToken.None);
            return run;
        }

        public async Task<AiPipelineStage> AddStageAsync(
            AiPipelineRun run, AiPipelineStageKind kind, AiPipelineStageStatus status, int attempts = 0)
        {
            var stage = new AiPipelineStage
            {
                Id = Guid.NewGuid(), RunId = run.Id, Kind = kind, Status = status, Attempts = attempts,
                MaxAttempts = 3, CreatedAt = DateTime.UtcNow
            };
            DbContext.AiPipelineStages.Add(stage);
            await DbContext.SaveChangesAsync(CancellationToken.None);
            return stage;
        }

        public async Task<AiArtifact> AddStrategyArtifactAsync()
        {
            var artifact = new AiArtifact
            {
                Id = Guid.NewGuid(), TenantId = Tenant.Id, BrandProfileId = Brand.Id, CampaignId = Campaign.Id,
                Kind = AiArtifactKind.Strategy, Version = 1, IsCurrent = true, ContentJson = "{}", SchemaVersion = 1,
                CreatedAt = DateTime.UtcNow
            };
            DbContext.AiArtifacts.Add(artifact);
            await DbContext.SaveChangesAsync(CancellationToken.None);
            return artifact;
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
