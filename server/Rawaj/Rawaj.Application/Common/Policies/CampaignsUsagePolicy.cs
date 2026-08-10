using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;

namespace Rawaj.Application.Common.Policies;

public record CampaignsUsage(int MaxCampaignsMonthly, int UsedThisMonth)
{
    public bool HasCampaignsRemaining => UsedThisMonth < MaxCampaignsMonthly;
}

/// <summary>
/// Same monthly-count rule <see cref="Rawaj.Application.Features.Campaigns.CreateCampaign.CreateCampaignCommandHandler"/>
/// enforces at create time, exposed as a read so the frontend can block the onboarding wizard
/// before the user spends seven steps on a campaign the backend will reject anyway.
/// </summary>
public static class CampaignsUsagePolicy
{
    public static async Task<CampaignsUsage> GetUsageAsync(IApplicationDbContext dbContext, Guid tenantId, CancellationToken cancellationToken)
    {
        var maxCampaignsMonthly = await (
            from tenant in dbContext.Tenants
            join subscription in dbContext.Subscriptions on tenant.SubscriptionId equals subscription.Id
            join plan in dbContext.SubscriptionPlans on subscription.SubscriptionPlanId equals plan.Id
            where tenant.Id == tenantId
            select plan.MaxCampaignsMonthly
        ).FirstAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var usedThisMonth = await dbContext.MarketingCampaigns
            .Where(c => c.BrandProfile.TenantId == tenantId && c.CreatedAt >= monthStart)
            .CountAsync(cancellationToken);

        return new CampaignsUsage(maxCampaignsMonthly, usedThisMonth);
    }
}
