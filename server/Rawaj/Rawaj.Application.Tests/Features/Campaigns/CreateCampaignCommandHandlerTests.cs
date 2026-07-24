using NSubstitute;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Features.Campaigns.CreateCampaign;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Persistence;
using Xunit;

namespace Rawaj.Application.Tests.Features.Campaigns;

public class CreateCampaignCommandHandlerTests
{
    private static async Task<(AppDbContext DbContext, Guid TenantId, Guid BrandProfileId)> SeedAsync(
        decimal planCost, int maxCampaignsMonthly)
    {
        var dbContext = TestDbContextFactory.Create();
        var planId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var brandProfileId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        dbContext.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = planCost > 0 ? "Pro" : "Free",
            Cost = planCost,
            Currency = "USD",
            MaxCampaignsMonthly = maxCampaignsMonthly,
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

        return (dbContext, tenantId, brandProfileId);
    }

    private static CreateCampaignCommandHandler BuildHandler(AppDbContext dbContext, Guid tenantId, Guid userId)
    {
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns(userId);

        var currentTenantContext = Substitute.For<ICurrentTenantContext>();
        currentTenantContext.TenantId.Returns(tenantId);

        return new CreateCampaignCommandHandler(dbContext, currentUserService, currentTenantContext);
    }

    [Fact]
    public async Task Handle_WithFreePlan_AllowsCampaignCreation()
    {
        // Free plan no longer gates campaign creation behind a paid subscription — it now allows
        // up to its own MaxCampaignsMonthly cap (seeded as 1 in production), same as any paid plan.
        var (dbContext, tenantId, brandProfileId) = await SeedAsync(planCost: 0, maxCampaignsMonthly: 10);
        var handler = BuildHandler(dbContext, tenantId, Guid.NewGuid());

        var result = await handler.Handle(
            new CreateCampaignCommand(brandProfileId, "Campaign", null, [], null, null, null, null),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(CampaignStatus.Draft, result.Data!.Status);
        Assert.Single(dbContext.MarketingCampaigns);
    }

    [Fact]
    public async Task Handle_WithFreePlanAtMonthlyLimit_RejectsCampaignCreation()
    {
        // Free plan is seeded with MaxCampaignsMonthly = 1 in production — confirm the cap is still
        // enforced even though the paid-plan gate is gone.
        var (dbContext, tenantId, brandProfileId) = await SeedAsync(planCost: 0, maxCampaignsMonthly: 1);
        var handler = BuildHandler(dbContext, tenantId, Guid.NewGuid());

        var first = await handler.Handle(
            new CreateCampaignCommand(brandProfileId, "First", null, [], null, null, null, null),
            CancellationToken.None);
        Assert.True(first.Succeeded);

        var second = await handler.Handle(
            new CreateCampaignCommand(brandProfileId, "Second", null, [], null, null, null, null),
            CancellationToken.None);

        Assert.False(second.Succeeded);
        Assert.Contains("maximum of 1 campaign", second.ErrorMessage);
    }

    [Fact]
    public async Task Handle_WithPaidPlanUnderLimit_CreatesCampaign()
    {
        var (dbContext, tenantId, brandProfileId) = await SeedAsync(planCost: 29, maxCampaignsMonthly: 10);
        var handler = BuildHandler(dbContext, tenantId, Guid.NewGuid());

        var result = await handler.Handle(
            new CreateCampaignCommand(brandProfileId, "Campaign", "Grow reach", ["Facebook"], null, null, null, null),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(CampaignStatus.Draft, result.Data!.Status);
        Assert.Single(dbContext.MarketingCampaigns);
    }

    [Fact]
    public async Task Handle_WithPaidPlanAtMonthlyLimit_RejectsCampaignCreation()
    {
        var (dbContext, tenantId, brandProfileId) = await SeedAsync(planCost: 29, maxCampaignsMonthly: 1);
        var handler = BuildHandler(dbContext, tenantId, Guid.NewGuid());

        var first = await handler.Handle(
            new CreateCampaignCommand(brandProfileId, "First", null, [], null, null, null, null),
            CancellationToken.None);
        Assert.True(first.Succeeded);

        var second = await handler.Handle(
            new CreateCampaignCommand(brandProfileId, "Second", null, [], null, null, null, null),
            CancellationToken.None);

        Assert.False(second.Succeeded);
        Assert.Contains("maximum of 1 campaign", second.ErrorMessage);
    }

    [Fact]
    public async Task Handle_WithBrandProfileFromAnotherTenant_ReturnsNotFound()
    {
        var (dbContext, _, brandProfileId) = await SeedAsync(planCost: 29, maxCampaignsMonthly: 10);
        var otherTenantId = Guid.NewGuid();
        var handler = BuildHandler(dbContext, otherTenantId, Guid.NewGuid());

        var result = await handler.Handle(
            new CreateCampaignCommand(brandProfileId, "Campaign", null, [], null, null, null, null),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Brand profile not found.", result.ErrorMessage);
    }
}
