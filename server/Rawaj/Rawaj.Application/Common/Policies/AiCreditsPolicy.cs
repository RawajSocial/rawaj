using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;

namespace Rawaj.Application.Common.Policies;

public record AiCreditsUsage(int MaxCreditsMonthly, int UsedThisMonth)
{
    public bool HasCreditsRemaining => UsedThisMonth < MaxCreditsMonthly;
}

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

        var usedThisMonth = await (
            from job in dbContext.AiJobs
            join brandProfile in dbContext.TenantBrandProfiles on job.BrandProfileId equals brandProfile.Id
            where brandProfile.TenantId == tenantId && job.CreatedAt >= monthStart
            select job.Id
        ).CountAsync(cancellationToken);

        return new AiCreditsUsage(maxAiCredits, usedThisMonth);
    }
}
