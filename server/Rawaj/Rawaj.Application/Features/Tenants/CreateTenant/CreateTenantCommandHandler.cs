using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Tenants.CreateTenant;

public class CreateTenantCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateTenantCommand, Result<CreateTenantResponse>>
{
    private const string DefaultPlanName = "Free";
    private const int TrialDays = 14;

    public async Task<Result<CreateTenantResponse>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Result<CreateTenantResponse>.Failure("Unauthorized.");
        }

        var alreadyOwnsTenant = await dbContext.Tenants
            .AnyAsync(t => t.OwnerUserId == userId, cancellationToken);
        if (alreadyOwnsTenant)
        {
            return Result<CreateTenantResponse>.Failure(
                "You already own an organization. Upgrade to a Marketing Agency subscription to manage multiple organizations.");
        }

        var subdomain = request.Subdomain.ToLowerInvariant();
        var subdomainTaken = await dbContext.Tenants
            .AnyAsync(t => t.Subdomain == subdomain, cancellationToken);
        if (subdomainTaken)
        {
            return Result<CreateTenantResponse>.Failure("This subdomain is already taken.");
        }

        var freePlan = await dbContext.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Name == DefaultPlanName && p.IsActive, cancellationToken);
        if (freePlan is null)
        {
            return Result<CreateTenantResponse>.Failure("No subscription plan is available.");
        }

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
            Name = request.Name,
            Subdomain = subdomain,
            TenantType = request.TenantType,
            OwnerUserId = userId.Value,
            SubscriptionId = subscription.Id,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var tenantMember = new TenantMember
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = userId.Value,
            Role = TenantMemberRole.Owner,
            InvitationStatus = InvitationStatus.Accepted,
            JoinedAt = now,
            CreatedAt = now
        };

        dbContext.Subscriptions.Add(subscription);
        dbContext.Tenants.Add(tenant);
        dbContext.TenantMembers.Add(tenantMember);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<CreateTenantResponse>.Success(
            new CreateTenantResponse(tenant.Id, tenant.Name, tenant.Subdomain, tenant.TenantType, subscription.Id));
    }
}
