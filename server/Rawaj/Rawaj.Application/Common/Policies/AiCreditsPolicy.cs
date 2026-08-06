using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;

namespace Rawaj.Application.Common.Policies;

public record AiCreditsUsage(int MaxCreditsMonthly, int UsedThisMonth)
{
    public bool HasCreditsRemaining => UsedThisMonth < MaxCreditsMonthly;
}

/// <summary>
/// The monthly AI-credits quota — a narrower gate than coins, kept only for the two features that
/// have no coin cost of their own (<c>AnalyzeCompetitor</c>, <c>ScrapeWebsite</c>; both call
/// Tavily, a paid API, with nothing else limiting how often a tenant can call them). Everywhere a
/// coin cost already exists, coins are the meter and this policy is not consulted — see
/// <c>AI_PIPELINE_V2_PLAN.md</c> Decision 5 for the retirement this file's history refers to.
/// </summary>
public static class AiCreditsPolicy
{
    public static async Task<AiCreditsUsage> GetUsageAsync(IApplicationDbContext dbContext, Guid tenantId, CancellationToken cancellationToken)
    {
        var maxAiCredits = await (
            from tenant in dbContext.Tenants
            join subscription in dbContext.Subscriptions on tenant.SubscriptionId equals subscription.Id
            join plan in dbContext.SubscriptionPlans on subscription.SubscriptionPlanId equals plan.Id
            where tenant.Id == tenantId
            select plan.MaxAiCreditsMonthly
        ).FirstAsync(cancellationToken);

        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Filters on AiJob.TenantId directly (C3) rather than inner-joining through
        // TenantBrandProfile — the join silently excluded every brand-less trial job from the
        // count, understating usage for exactly the tenants a monthly cap matters most for.
        var usedThisMonth = await dbContext.AiJobs
            .Where(j => j.TenantId == tenantId && j.CreatedAt >= monthStart)
            .CountAsync(cancellationToken);

        return new AiCreditsUsage(maxAiCredits, usedThisMonth);
    }
}
