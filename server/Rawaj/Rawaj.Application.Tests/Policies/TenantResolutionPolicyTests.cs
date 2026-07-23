using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Xunit;

namespace Rawaj.Application.Tests.Policies;

public class TenantResolutionPolicyTests
{
    private static async Task<Guid> SeedTenantAsync(
        Rawaj.Persistence.AppDbContext dbContext, Guid ownerUserId, Guid memberUserId, TenantMemberRole memberRole,
        InvitationStatus status = InvitationStatus.Accepted)
    {
        var planId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        dbContext.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId, Name = Guid.NewGuid().ToString("N"), Cost = 10, Currency = "USD", IsActive = true, CreatedAt = now
        });
        dbContext.Subscriptions.Add(new Subscription
        {
            Id = subscriptionId, SubscriptionPlanId = planId, Status = SubscriptionStatus.Active,
            CurrentPeriodStart = now, CurrentPeriodEnd = now.AddMonths(1), CreatedAt = now
        });
        dbContext.Tenants.Add(new Tenant
        {
            Id = tenantId, Name = "Tenant " + tenantId, Subdomain = Guid.NewGuid().ToString("N"),
            TenantType = TenantType.Business, OwnerUserId = ownerUserId, SubscriptionId = subscriptionId,
            IsActive = true, CreatedAt = now, UpdatedAt = now
        });
        dbContext.TenantMembers.Add(new TenantMember
        {
            Id = Guid.NewGuid(), TenantId = tenantId, UserId = memberUserId, Role = memberRole,
            InvitationStatus = status, JoinedAt = status == InvitationStatus.Accepted ? now : null, CreatedAt = now
        });

        await dbContext.SaveChangesAsync(CancellationToken.None);
        return tenantId;
    }

    [Fact]
    public async Task ResolveAsync_NoTenantRequested_PrefersTheUsersOwnTenant()
    {
        using var dbContext = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();

        // The user owns one tenant and is an invited Editor on another.
        var ownTenantId = await SeedTenantAsync(dbContext, ownerUserId: userId, memberUserId: userId, TenantMemberRole.Owner);
        await SeedTenantAsync(dbContext, ownerUserId: Guid.NewGuid(), memberUserId: userId, TenantMemberRole.Editor);

        var resolved = await TenantResolutionPolicy.ResolveAsync(dbContext, userId, requestedTenantId: null, CancellationToken.None);

        Assert.NotNull(resolved);
        Assert.Equal(ownTenantId, resolved.TenantId);
        Assert.Equal(TenantMemberRole.Owner, resolved.Role);
    }

    [Fact]
    public async Task ResolveAsync_RequestingAnAcceptedMembership_HonoursIt()
    {
        using var dbContext = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();

        await SeedTenantAsync(dbContext, ownerUserId: userId, memberUserId: userId, TenantMemberRole.Owner);
        var invitedTenantId = await SeedTenantAsync(dbContext, ownerUserId: Guid.NewGuid(), memberUserId: userId, TenantMemberRole.Editor);

        var resolved = await TenantResolutionPolicy.ResolveAsync(dbContext, userId, invitedTenantId, CancellationToken.None);

        Assert.NotNull(resolved);
        Assert.Equal(invitedTenantId, resolved.TenantId);
        Assert.Equal(TenantMemberRole.Editor, resolved.Role);
    }

    [Fact]
    public async Task ResolveAsync_RequestingATenantTheUserIsNotAMemberOf_Throws()
    {
        using var dbContext = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, ownerUserId: userId, memberUserId: userId, TenantMemberRole.Owner);

        var foreignTenantId = Guid.NewGuid();

        await Assert.ThrowsAsync<Rawaj.Application.Common.Exceptions.ForbiddenAccessException>(
            () => TenantResolutionPolicy.ResolveAsync(dbContext, userId, foreignTenantId, CancellationToken.None));
    }

    [Fact]
    public async Task ResolveAsync_PendingMembershipIsIgnored()
    {
        using var dbContext = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();

        var pendingTenantId = await SeedTenantAsync(
            dbContext, ownerUserId: Guid.NewGuid(), memberUserId: userId, TenantMemberRole.Editor, InvitationStatus.Pending);

        var resolved = await TenantResolutionPolicy.ResolveAsync(dbContext, userId, requestedTenantId: null, CancellationToken.None);

        Assert.Null(resolved);

        await Assert.ThrowsAsync<Rawaj.Application.Common.Exceptions.ForbiddenAccessException>(
            () => TenantResolutionPolicy.ResolveAsync(dbContext, userId, pendingTenantId, CancellationToken.None));
    }
}
