using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Domain.ValueObjects;

namespace Rawaj.Application.Common.Services;

/// <summary>
/// Shared tenant + subscription + owner-membership creation logic, used both by the explicit
/// CreateTenant endpoint and by registration's automatic tenant provisioning.
/// </summary>
public class TenantProvisioningService(IApplicationDbContext dbContext)
{
    private const string DefaultPlanName = "Free";
    private const int TrialDays = 14;
    public const int StartingCoinBalance = 100;

    public async Task<Tenant> ProvisionAsync(
        Guid ownerUserId,
        string name,
        string subdomain,
        TenantType tenantType,
        CancellationToken cancellationToken,
        bool createDefaultBrandProfile = false)
    {
        var freePlan = await dbContext.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Name == DefaultPlanName && p.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("No default subscription plan is available.");

        var now = DateTime.UtcNow;

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            SubscriptionPlanId = freePlan.Id,
            BillingCycle = BillingCycle.Monthly,
            Status = SubscriptionStatus.Trialing,
            CurrentPeriodStart = now,
            CurrentPeriodEnd = now.AddDays(TrialDays),
            TrialEndsAt = now.AddDays(TrialDays),
            CreatedAt = now
        };

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Subdomain = subdomain,
            TenantType = tenantType,
            OwnerUserId = ownerUserId,
            SubscriptionId = subscription.Id,
            IsActive = true,
            CoinBalance = StartingCoinBalance,
            IsActivated = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        var tenantMember = new TenantMember
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = ownerUserId,
            Role = TenantMemberRole.Owner,
            InvitationStatus = InvitationStatus.Accepted,
            JoinedAt = now,
            CreatedAt = now
        };

        dbContext.Subscriptions.Add(subscription);
        dbContext.Tenants.Add(tenant);
        dbContext.TenantMembers.Add(tenantMember);

        if (createDefaultBrandProfile)
        {
            var brandProfile = new TenantBrandProfile
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Name = name,
                Status = BrandProfileStatus.Active,
                BrandInfo = new BrandInfo { IsDefault = true },
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.TenantBrandProfiles.Add(brandProfile);
        }

        return tenant;
    }

    public static string GenerateSubdomain(string emailOrName)
    {
        var slug = new string((emailOrName.Split('@')[0])
            .ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .ToArray());

        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "tenant";
        }

        return $"{slug}-{Guid.NewGuid().ToString("N")[..8]}";
    }
}
