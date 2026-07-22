using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Enums;
using Rawaj.Domain.ValueObjects;

namespace Rawaj.Application.Features.Tenants.UpgradeToAgency;

public class UpgradeToAgencyCommandHandler(
    IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<UpgradeToAgencyCommand, Result<UpgradeToAgencyResponse>>
{
    private const string AgencyPlanName = "Pro";
    private const int TrialDays = 14;

    public async Task<Result<UpgradeToAgencyResponse>> Handle(
        UpgradeToAgencyCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var tenant = await dbContext.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant.TenantType == TenantType.Agency)
        {
            return Result<UpgradeToAgencyResponse>.Failure("This organization is already a marketing agency.");
        }

        var agencyPlan = await dbContext.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Name == AgencyPlanName && p.IsActive, cancellationToken);
        if (agencyPlan is null)
        {
            return Result<UpgradeToAgencyResponse>.Failure("No agency subscription plan is available.");
        }

        var currentSubscription = await dbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == tenant.SubscriptionId, cancellationToken);

        var now = DateTime.UtcNow;

        if (currentSubscription is not null)
        {
            currentSubscription.Status = SubscriptionStatus.Cancelled;
            currentSubscription.CancelledAt = now;
        }

        var newSubscription = new Subscription
        {
            Id = Guid.NewGuid(),
            SubscriptionPlanId = agencyPlan.Id,
            BillingCycle = BillingCycle.Monthly,
            Status = SubscriptionStatus.Trialing,
            CurrentPeriodStart = now,
            CurrentPeriodEnd = now.AddDays(TrialDays),
            TrialEndsAt = now.AddDays(TrialDays),
            CreatedAt = now
        };
        dbContext.Subscriptions.Add(newSubscription);

        var profile = tenant.TenantProfile ?? new TenantProfile();
        profile.Phone = request.Phone ?? profile.Phone;
        profile.Industry = request.Industry ?? profile.Industry;
        profile.Country = request.Country ?? profile.Country;
        profile.City = request.City ?? profile.City;
        profile.Website = request.Website ?? profile.Website;
        profile.AgencySize = request.AgencySize;
        profile.ServicesOffered = request.ServicesOffered;
        tenant.TenantProfile = profile;

        tenant.TenantType = TenantType.Agency;
        tenant.SubscriptionId = newSubscription.Id;
        tenant.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<UpgradeToAgencyResponse>.Success(
            new UpgradeToAgencyResponse(tenant.Id, tenant.TenantType, agencyPlan.Name, agencyPlan.MaxBrands));
    }
}
