using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Xunit;

namespace Rawaj.Application.Tests.Policies;

public class AiCreditsPolicyTests
{
    private static async Task<(Guid TenantId, Guid BrandProfileId)> SeedTenantWithPlanAsync(
        Rawaj.Persistence.AppDbContext dbContext, int maxAiCreditsMonthly)
    {
        var planId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var brandProfileId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        dbContext.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "TestPlan",
            Cost = 10,
            Currency = "USD",
            MaxAiCreditsMonthly = maxAiCreditsMonthly,
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
            TenantType = TenantType.Agency,
            OwnerUserId = Guid.NewGuid(),
            SubscriptionId = subscriptionId,
            IsActive = true,
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

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return (tenantId, brandProfileId);
    }

    [Fact]
    public async Task GetUsageAsync_WithNoJobsThisMonth_ReportsFullCreditsRemaining()
    {
        using var dbContext = TestDbContextFactory.Create();
        var (tenantId, _) = await SeedTenantWithPlanAsync(dbContext, maxAiCreditsMonthly: 5);

        var usage = await AiCreditsPolicy.GetUsageAsync(dbContext, tenantId, CancellationToken.None);

        Assert.Equal(5, usage.MaxCreditsMonthly);
        Assert.Equal(0, usage.UsedThisMonth);
        Assert.True(usage.HasCreditsRemaining);
    }

    [Fact]
    public async Task GetUsageAsync_CountsOnlyJobsFromThisMonth()
    {
        using var dbContext = TestDbContextFactory.Create();
        var (tenantId, brandProfileId) = await SeedTenantWithPlanAsync(dbContext, maxAiCreditsMonthly: 3);

        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        dbContext.AiJobs.Add(new AiJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BrandProfileId = brandProfileId,
            TriggeredBy = Guid.NewGuid(),
            JobType = AiJobType.ContentGeneration,
            Status = AiJobStatus.Completed,
            CreatedAt = monthStart.AddDays(1)
        });
        dbContext.AiJobs.Add(new AiJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BrandProfileId = brandProfileId,
            TriggeredBy = Guid.NewGuid(),
            JobType = AiJobType.ImageGeneration,
            Status = AiJobStatus.Completed,
            CreatedAt = monthStart.AddMonths(-1) // last month - should not count
        });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var usage = await AiCreditsPolicy.GetUsageAsync(dbContext, tenantId, CancellationToken.None);

        Assert.Equal(1, usage.UsedThisMonth);
        Assert.True(usage.HasCreditsRemaining);
    }

    [Fact]
    public async Task GetUsageAsync_WhenUsedMeetsMax_HasNoCreditsRemaining()
    {
        using var dbContext = TestDbContextFactory.Create();
        var (tenantId, brandProfileId) = await SeedTenantWithPlanAsync(dbContext, maxAiCreditsMonthly: 1);

        dbContext.AiJobs.Add(new AiJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BrandProfileId = brandProfileId,
            TriggeredBy = Guid.NewGuid(),
            JobType = AiJobType.ContentGeneration,
            Status = AiJobStatus.Completed,
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var usage = await AiCreditsPolicy.GetUsageAsync(dbContext, tenantId, CancellationToken.None);

        Assert.False(usage.HasCreditsRemaining);
    }

    /// <summary>
    /// C23: the count used to inner-join through <c>TenantBrandProfile</c>, so a brand-less trial
    /// job (<c>AiJob.BrandProfileId == null</c>) was silently invisible to it. Filtering on
    /// <c>AiJob.TenantId</c> directly (set on every job since C3, regardless of brand) fixes that —
    /// this test would have failed against the old join.
    /// </summary>
    [Fact]
    public async Task GetUsageAsync_CountsBrandlessTrialJobs()
    {
        using var dbContext = TestDbContextFactory.Create();
        var (tenantId, _) = await SeedTenantWithPlanAsync(dbContext, maxAiCreditsMonthly: 2);

        dbContext.AiJobs.Add(new AiJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BrandProfileId = null,
            TriggeredBy = Guid.NewGuid(),
            JobType = AiJobType.ContentGeneration,
            Status = AiJobStatus.Completed,
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var usage = await AiCreditsPolicy.GetUsageAsync(dbContext, tenantId, CancellationToken.None);

        Assert.Equal(1, usage.UsedThisMonth);
    }
}
