using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Persistence;
using Xunit;

namespace Rawaj.Application.Tests.Policies;

/// <summary>
/// Coin charging for pipeline stages. The pipeline splits work far more finely than the handlers it
/// replaces, and it retries far more readily, so the two things these tests exist to guarantee are
/// that the extra stages cost the tenant nothing extra and that a retry can never charge twice.
/// </summary>
public class AiPipelineCoinPolicyTests
{
    /// <summary>The real configured base costs, so the totals here are the totals users pay.</summary>
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

    [Fact]
    public async Task AFullRun_Costs20500_TheSameAsTheHandlersItReplaces()
    {
        // The load-bearing test of this commit. Today a full campaign costs
        // 3,500 (research) + 4,000 (diagnosis) + 12,000 (strategy) + 1,000 (content) = 20,500.
        // The pipeline has eleven stages instead of four handlers, and must still cost exactly that.
        await using var dbContext = TestDbContextFactory.Create();
        var (tenantId, _, campaignId) = await SeedAsync(dbContext, coinBalance: 100_000, freeMarketingPlanUsed: true);
        var (run, stages) = CreateRun(tenantId, campaignId);

        var total = 0;
        foreach (var stage in stages)
        {
            var result = await AiPipelineCoinPolicy.TryChargeAsync(
                dbContext, stage, run, run.TriggeredBy, TenantMemberRole.Owner, Costs, CancellationToken.None);
            total += result.Charged;
        }

        Assert.Equal(20_500, total);
        Assert.Equal(20_500, run.TotalCoinsSpent);
    }

    [Fact]
    public async Task TheNewStages_AreFree()
    {
        // Brand analysis, market research and the three strategy sub-tasks are capability the product
        // did not have. They are folded into the existing charge points rather than priced.
        await using var dbContext = TestDbContextFactory.Create();
        var (tenantId, _, campaignId) = await SeedAsync(dbContext, coinBalance: 100_000, freeMarketingPlanUsed: true);
        var (run, stages) = CreateRun(tenantId, campaignId);

        AiPipelineStageKind[] shouldBeFree =
        [
            AiPipelineStageKind.BrandAnalysis,
            AiPipelineStageKind.MarketResearch,
            AiPipelineStageKind.StrategyPositioning,
            AiPipelineStageKind.StrategyBlueprint,
            AiPipelineStageKind.StrategyRoadmap,
            AiPipelineStageKind.HumanApproval
        ];

        foreach (var kind in shouldBeFree)
        {
            var stage = stages.Single(s => s.Kind == kind);
            var result = await AiPipelineCoinPolicy.TryChargeAsync(
                dbContext, stage, run, run.TriggeredBy, TenantMemberRole.Owner, Costs, CancellationToken.None);

            Assert.Equal(0, result.Charged);
            Assert.False(AiPipelineCoinPolicy.IsChargePoint(kind));
        }
    }

    [Fact]
    public async Task ImageStages_AreFree_BecauseTheBatchFeeAlreadyIncludesThem()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var (tenantId, _, campaignId) = await SeedAsync(dbContext, coinBalance: 100_000, freeMarketingPlanUsed: true);
        var (run, _) = CreateRun(tenantId, campaignId);

        var imageStage = Stage(run.Id, AiPipelineStageKind.ContentImage);
        imageStage.TargetRefId = Guid.NewGuid();

        var result = await AiPipelineCoinPolicy.TryChargeAsync(
            dbContext, imageStage, run, run.TriggeredBy, TenantMemberRole.Owner, Costs, CancellationToken.None);

        Assert.Equal(0, result.Charged);
    }

    [Fact]
    public async Task Retrying_AChargedStage_DoesNotChargeAgain()
    {
        // The failure this centralisation exists to prevent. A stage that completed, was charged, and
        // is then re-run — by a resume, a manual single-stage invocation, or two workers racing —
        // must never produce a second debit.
        await using var dbContext = TestDbContextFactory.Create();
        var (tenantId, _, campaignId) = await SeedAsync(dbContext, coinBalance: 100_000, freeMarketingPlanUsed: true);
        var (run, stages) = CreateRun(tenantId, campaignId);
        var contentPlan = stages.Single(s => s.Kind == AiPipelineStageKind.ContentPlan);

        var first = await AiPipelineCoinPolicy.TryChargeAsync(
            dbContext, contentPlan, run, run.TriggeredBy, TenantMemberRole.Owner, Costs, CancellationToken.None);
        var second = await AiPipelineCoinPolicy.TryChargeAsync(
            dbContext, contentPlan, run, run.TriggeredBy, TenantMemberRole.Owner, Costs, CancellationToken.None);
        var third = await AiPipelineCoinPolicy.TryChargeAsync(
            dbContext, contentPlan, run, run.TriggeredBy, TenantMemberRole.Owner, Costs, CancellationToken.None);

        Assert.Equal(1_000, first.Charged);
        Assert.Equal(0, second.Charged);
        Assert.Equal(0, third.Charged);
        Assert.True(second.AlreadyCharged);
        Assert.Equal(1_000, run.TotalCoinsSpent);

        var balance = await dbContext.Tenants.Where(t => t.Id == tenantId).Select(t => t.CoinBalance).FirstAsync();
        Assert.Equal(99_000, balance);
    }

    [Fact]
    public async Task CompetitorResearch_ThatFoundNothing_IsFree()
    {
        // Preserves today's rule exactly: an empty or failed Tavily result is recorded and the flow
        // continues, but no research value was delivered so nothing is charged.
        await using var dbContext = TestDbContextFactory.Create();
        var (tenantId, _, campaignId) = await SeedAsync(dbContext, coinBalance: 100_000, freeMarketingPlanUsed: true);
        var (run, stages) = CreateRun(tenantId, campaignId);
        var research = stages.Single(s => s.Kind == AiPipelineStageKind.CompetitorResearch);

        var result = await AiPipelineCoinPolicy.TryChargeAsync(
            dbContext, research, run, run.TriggeredBy, TenantMemberRole.Owner, Costs, CancellationToken.None,
            valueDelivered: false);

        Assert.Equal(0, result.Charged);
        Assert.Equal(0, research.CoinsCharged);
    }

    [Fact]
    public async Task TheFirstStrategy_IsFree_AndOnlyTheFirst()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var (tenantId, _, campaignId) = await SeedAsync(dbContext, coinBalance: 100_000, freeMarketingPlanUsed: false);
        var (firstRun, firstStages) = CreateRun(tenantId, campaignId);

        var free = await AiPipelineCoinPolicy.TryChargeAsync(
            dbContext, firstStages.Single(s => s.Kind == AiPipelineStageKind.StrategyAssemble),
            firstRun, firstRun.TriggeredBy, TenantMemberRole.Owner, Costs, CancellationToken.None);

        Assert.Equal(0, free.Charged);
        Assert.True(await dbContext.Tenants.Where(t => t.Id == tenantId).Select(t => t.FreeMarketingPlanUsed).FirstAsync());

        var (secondRun, secondStages) = CreateRun(tenantId, campaignId);
        var paid = await AiPipelineCoinPolicy.TryChargeAsync(
            dbContext, secondStages.Single(s => s.Kind == AiPipelineStageKind.StrategyAssemble),
            secondRun, secondRun.TriggeredBy, TenantMemberRole.Owner, Costs, CancellationToken.None);

        Assert.Equal(12_000, paid.Charged);
    }

    [Fact]
    public async Task PlanDiscount_IsAppliedBeforeCharging()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var (tenantId, _, campaignId) = await SeedAsync(
            dbContext, coinBalance: 100_000, freeMarketingPlanUsed: true, discountPercent: 25);
        var (run, stages) = CreateRun(tenantId, campaignId);

        var result = await AiPipelineCoinPolicy.TryChargeAsync(
            dbContext, stages.Single(s => s.Kind == AiPipelineStageKind.CampaignAnalysis),
            run, run.TriggeredBy, TenantMemberRole.Owner, Costs, CancellationToken.None);

        Assert.Equal(3_000, result.Charged);
    }

    [Fact]
    public async Task ShortBalance_IsDetectedBeforeTheProviderIsCalled()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var (tenantId, _, _) = await SeedAsync(dbContext, coinBalance: 500, freeMarketingPlanUsed: true);

        var charge = await AiPipelineCoinPolicy.GetChargeAsync(
            dbContext, AiPipelineStageKind.ContentPlan, tenantId, Costs, CancellationToken.None);
        var affordable = await AiPipelineCoinPolicy.CanAffordAsync(
            dbContext, charge, tenantId, Guid.NewGuid(), TenantMemberRole.Owner, CancellationToken.None);

        Assert.Equal(1_000, charge.Cost);
        Assert.False(affordable);
    }

    [Fact]
    public async Task LedgerReasons_MatchTheOnesAlreadyInBillingHistory()
    {
        // Changing these would split a tenant's history into "before" and "after" entries for what is
        // the same action.
        await using var dbContext = TestDbContextFactory.Create();
        var (tenantId, _, campaignId) = await SeedAsync(dbContext, coinBalance: 100_000, freeMarketingPlanUsed: true);
        var (run, stages) = CreateRun(tenantId, campaignId);

        foreach (var stage in stages)
        {
            await AiPipelineCoinPolicy.TryChargeAsync(
                dbContext, stage, run, run.TriggeredBy, TenantMemberRole.Owner, Costs, CancellationToken.None);
        }

        var reasons = await dbContext.CoinLedgerEntries.Select(e => e.Reason).OrderBy(r => r).ToListAsync();

        Assert.Equal(
            ["business_diagnosis", "campaign_content_generation", "competitive_analysis", "marketing_plan_generation"],
            reasons);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────

    private static (AiPipelineRun Run, List<AiPipelineStage> Stages) CreateRun(Guid tenantId, Guid campaignId)
    {
        var run = new AiPipelineRun
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BrandProfileId = Guid.NewGuid(),
            CampaignId = campaignId,
            TriggeredBy = Guid.NewGuid(),
            Status = AiPipelineRunStatus.Running,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var stages = AiPipelinePolicy.InitialStages().Select(d => Stage(run.Id, d.Kind)).ToList();

        return (run, stages);
    }

    private static AiPipelineStage Stage(Guid runId, AiPipelineStageKind kind)
    {
        var definition = AiPipelinePolicy.Definition(kind);

        return new AiPipelineStage
        {
            Id = Guid.NewGuid(),
            RunId = runId,
            Kind = kind,
            Ordinal = definition.Ordinal,
            Status = AiPipelineStageStatus.Completed,
            IsOptional = definition.IsOptional,
            MaxAttempts = definition.MaxAttempts,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static async Task<(Guid TenantId, Guid BrandProfileId, Guid CampaignId)> SeedAsync(
        AppDbContext dbContext, int coinBalance, bool freeMarketingPlanUsed, int discountPercent = 0)
    {
        var now = DateTime.UtcNow;
        var planId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var brandProfileId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();

        dbContext.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "Test",
            Cost = 0,
            Currency = "USD",
            MaxCampaignsMonthly = 100,
            CoinUsageDiscountPercent = discountPercent,
            IsActive = true,
            CreatedAt = now
        });

        dbContext.Subscriptions.Add(new Subscription
        {
            Id = subscriptionId,
            SubscriptionPlanId = planId,
            Status = SubscriptionStatus.Active,
            CurrentPeriodStart = now,
            CurrentPeriodEnd = now.AddMonths(1),
            CreatedAt = now
        });

        dbContext.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Test Tenant",
            Subdomain = Guid.NewGuid().ToString("N"),
            TenantType = TenantType.Business,
            OwnerUserId = Guid.NewGuid(),
            SubscriptionId = subscriptionId,
            CoinBalance = coinBalance,
            FreeMarketingPlanUsed = freeMarketingPlanUsed,
            IsActive = true,
            IsActivated = true,
            CreatedAt = now,
            UpdatedAt = now
        });

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
            Status = CampaignStatus.Draft,
            TargetPlatforms = [],
            CreatedAt = now,
            UpdatedAt = now
        });

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return (tenantId, brandProfileId, campaignId);
    }
}
