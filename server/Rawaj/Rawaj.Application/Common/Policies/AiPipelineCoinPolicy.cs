using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Policies;

/// <summary>What a stage costs, and why.</summary>
/// <param name="Cost">The discounted price actually charged. Zero for a stage that is not a charge
/// point, and for the tenant's one free strategy.</param>
/// <param name="UsesFreeTrial">Whether this would consume the tenant's one free marketing plan.</param>
/// <param name="LedgerReason">The ledger reason string — deliberately the same values the existing
/// handlers write, so billing history stays readable across the cutover.</param>
public sealed record StageCharge(int Cost, bool UsesFreeTrial, string? LedgerReason);

/// <summary>Outcome of trying to charge for a stage.</summary>
/// <param name="Charged">Coins actually debited on this call. Zero when the stage is free, already
/// charged, or delivered nothing worth charging for.</param>
/// <param name="AlreadyCharged">The stage had a charge recorded already — a retry or a re-run.</param>
public sealed record StageChargeResult(int Charged, bool AlreadyCharged);

/// <summary>
/// Coin charging for pipeline stages: one place, instead of the same
/// discount → balance → call → charge-on-success sequence copy-pasted across five handlers.
///
/// <para><b>More stages must not mean more charges.</b> The pipeline splits work far more finely
/// than the handlers it replaces — brand analysis, market research and three strategy sub-tasks did
/// not exist before — so stages are grouped into <i>charge points</i>. Each group is charged exactly
/// once, on the stage that produces the group's user-visible deliverable, and the stages around it
/// are free. A full run still costs 20,500 coins, exactly as today
/// (see docs/AI_PIPELINE.md §7).</para>
///
/// <para>Every rule the current handlers enforce is preserved: the charge happens only after a
/// usable result exists, a failed stage is free, the plan discount applies before the balance check,
/// and the tenant's first complete strategy is free.</para>
/// </summary>
public static class AiPipelineCoinPolicy
{
    /// <summary>
    /// Which stage carries the charge for its group. Each is the stage that produces the thing the
    /// tenant is actually paying for, which is also what keeps "a failed stage is free" true without
    /// any extra bookkeeping: if the deliverable never materialises, its stage never completes, and
    /// nothing is charged — even if earlier stages in the group succeeded.
    ///
    /// <list type="bullet">
    /// <item>BrandAnalysis is free; CampaignAnalysis carries the business-understanding charge for
    /// both, matching today's single BusinessDiagnosis fee. Brand analysis is also frequently a
    /// cache hit, so charging on it would price the same work differently run to run.</item>
    /// <item>MarketResearch is free; it is new capability folded into the existing research budget
    /// rather than a new line item.</item>
    /// <item>StrategyAssemble carries the whole strategy fee — it is genuinely last, and a run whose
    /// sub-tasks never assemble into a readable strategy has produced nothing to bill for.</item>
    /// <item>ContentPlan carries the content fee and every ContentImage is free, matching today's
    /// flat batch price that already includes the images.</item>
    /// </list>
    /// </summary>
    private static readonly Dictionary<AiPipelineStageKind, string> ChargePoints = new()
    {
        [AiPipelineStageKind.CampaignAnalysis] = "business_diagnosis",
        [AiPipelineStageKind.CompetitorResearch] = "competitive_analysis",
        [AiPipelineStageKind.StrategyAssemble] = "marketing_plan_generation",
        [AiPipelineStageKind.ContentPlan] = "campaign_content_generation"
    };

    public static bool IsChargePoint(AiPipelineStageKind kind) => ChargePoints.ContainsKey(kind);

    public static string? LedgerReason(AiPipelineStageKind kind) =>
        ChargePoints.TryGetValue(kind, out var reason) ? reason : null;

    /// <summary>The action name that appears in the "not enough coins" message the user reads.</summary>
    public static string ActionDescription(AiPipelineStageKind kind) => kind switch
    {
        AiPipelineStageKind.CampaignAnalysis => "analyse this business",
        AiPipelineStageKind.CompetitorResearch => "run competitor research",
        AiPipelineStageKind.StrategyAssemble => "generate a marketing strategy",
        AiPipelineStageKind.ContentPlan => "generate campaign content",
        _ => "run this step"
    };

    private static int BaseCost(AiPipelineStageKind kind, ICoinCostProvider costs) => kind switch
    {
        AiPipelineStageKind.CampaignAnalysis => costs.BusinessDiagnosis,
        AiPipelineStageKind.CompetitorResearch => costs.CompetitiveAnalysis,
        AiPipelineStageKind.StrategyAssemble => costs.MarketingPlanGeneration,
        AiPipelineStageKind.ContentPlan => costs.CampaignContentGeneration,
        _ => 0
    };

    /// <summary>
    /// What this stage will cost, discount and free trial applied. Call before the provider is
    /// invoked so a tenant who cannot afford a stage is parked rather than charged for work that
    /// then can't be paid for.
    /// </summary>
    public static async Task<StageCharge> GetChargeAsync(
        IApplicationDbContext dbContext,
        AiPipelineStageKind kind,
        Guid tenantId,
        ICoinCostProvider costs,
        CancellationToken cancellationToken)
    {
        if (!IsChargePoint(kind))
        {
            return new StageCharge(0, false, null);
        }

        var reason = ChargePoints[kind];

        // The pricing sheet's one free complete strategy per tenant, preserved exactly.
        if (kind == AiPipelineStageKind.StrategyAssemble)
        {
            var freeUsed = await dbContext.Tenants
                .Where(t => t.Id == tenantId)
                .Select(t => t.FreeMarketingPlanUsed)
                .FirstAsync(cancellationToken);

            if (!freeUsed)
            {
                return new StageCharge(0, true, reason);
            }
        }

        var cost = await CoinPricingPolicy.GetDiscountedCostAsync(
            dbContext, tenantId, BaseCost(kind, costs), cancellationToken);

        return new StageCharge(cost, false, reason);
    }

    /// <summary>
    /// Whether the wallet can cover this stage. Separate from charging because the check belongs
    /// before the provider call and the charge belongs after it.
    /// </summary>
    public static async Task<bool> CanAffordAsync(
        IApplicationDbContext dbContext,
        StageCharge charge,
        Guid tenantId,
        Guid userId,
        TenantMemberRole role,
        CancellationToken cancellationToken)
    {
        if (charge.Cost <= 0)
        {
            return true;
        }

        var balance = await CoinPolicy.GetBalanceAsync(dbContext, tenantId, userId, role, cancellationToken);
        return balance >= charge.Cost;
    }

    /// <summary>
    /// The message shown when the wallet is short. Goes through <see cref="CoinPolicy"/> so the
    /// exact sentence shape the frontend parses for its "buy coins" link is unchanged.
    /// </summary>
    public static async Task<string> InsufficientMessageAsync(
        IApplicationDbContext dbContext,
        StageCharge charge,
        AiPipelineStageKind kind,
        Guid tenantId,
        Guid userId,
        TenantMemberRole role,
        CancellationToken cancellationToken)
    {
        var balance = await CoinPolicy.GetBalanceAsync(dbContext, tenantId, userId, role, cancellationToken);
        return CoinPolicy.InsufficientCoinsMessage(charge.Cost, balance, ActionDescription(kind));
    }

    /// <summary>
    /// Charges for a completed stage, once and only once.
    ///
    /// <para>Idempotency is the point. <see cref="AiPipelineStage.CoinsCharged"/> is the guard, so a
    /// retried stage, a re-run of a completed one, or two workers racing on the same row can never
    /// produce a second debit — which is the failure mode that made this worth centralising, since
    /// the pipeline retries far more readily than the handlers it replaces.</para>
    /// </summary>
    /// <param name="valueDelivered">False when the stage completed without producing anything worth
    /// paying for. Competitor research uses this: it charges only when it actually found data, and
    /// an empty result is recorded and left free, exactly as today.</param>
    public static async Task<StageChargeResult> TryChargeAsync(
        IApplicationDbContext dbContext,
        AiPipelineStage stage,
        AiPipelineRun run,
        Guid userId,
        TenantMemberRole role,
        ICoinCostProvider costs,
        CancellationToken cancellationToken,
        bool valueDelivered = true)
    {
        if (stage.CoinsCharged > 0)
        {
            return new StageChargeResult(0, AlreadyCharged: true);
        }

        if (!IsChargePoint(stage.Kind) || !valueDelivered)
        {
            return new StageChargeResult(0, AlreadyCharged: false);
        }

        var charge = await GetChargeAsync(dbContext, stage.Kind, run.TenantId, costs, cancellationToken);

        if (charge.UsesFreeTrial)
        {
            var tenant = await dbContext.Tenants.FirstAsync(t => t.Id == run.TenantId, cancellationToken);
            tenant.FreeMarketingPlanUsed = true;

            // Committed here rather than left to the caller's later SaveChangesAsync, matching how
            // CoinPolicy.TrySpendAsync commits a debit immediately. Consuming the free strategy is a
            // balance change in everything but name: if the caller fails after this point and never
            // saves, the tenant silently gets a second free 12,000-coin strategy.
            await dbContext.SaveChangesAsync(cancellationToken);

            // Still zero charged: the run's coin rollup counts coins, and this cost none.
            return new StageChargeResult(0, AlreadyCharged: false);
        }

        if (charge.Cost <= 0)
        {
            return new StageChargeResult(0, AlreadyCharged: false);
        }

        var spent = await CoinPolicy.TrySpendAsync(
            dbContext, run.TenantId, userId, role, charge.Cost, cancellationToken,
            reason: charge.LedgerReason!, referenceId: run.CampaignId, referenceType: "marketing_campaign");

        if (!spent)
        {
            // Lost a race with another spend since the pre-flight check. The caller treats this as
            // AiFailureKind.Coins, which parks the run without consuming an attempt.
            return new StageChargeResult(0, AlreadyCharged: false);
        }

        stage.CoinsCharged = charge.Cost;

        // Atomic, not a tracked-entity `run.TotalCoinsSpent += charge.Cost`: two stages from the same
        // run's fan-out (e.g. Positioning + Blueprint, dispatched together) can each reach this in
        // their own isolated scope at nearly the same moment, and a read-then-increment on separately
        // loaded copies of `run` would lose whichever one saves first. This also keeps `run` itself
        // out of the caller's next SaveChangesAsync entirely when this is the only thing that would
        // have touched it — avoiding a spurious DbUpdateConcurrencyException against a RowVersion that
        // a sibling scope's own Version bump (see AiPipelineRun.Version) may have already advanced.
        try
        {
            await dbContext.AiPipelineRuns
                .Where(r => r.Id == run.Id)
                .ExecuteUpdateAsync(r => r.SetProperty(x => x.TotalCoinsSpent, x => x.TotalCoinsSpent + charge.Cost), cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // EF Core's InMemory provider (test suite only) doesn't translate ExecuteUpdate — see the
            // identical caveat on PipelineOrchestrator's Version bump. InMemory never runs more than
            // one writer at a time, so a direct increment is equivalent there.
            run.TotalCoinsSpent += charge.Cost;
        }

        return new StageChargeResult(charge.Cost, AlreadyCharged: false);
    }
}
