using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Services;

namespace Rawaj.Application.Features.Tenants.CreateTenant;

public class CreateTenantCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    TenantProvisioningService tenantProvisioningService)
    : IRequestHandler<CreateTenantCommand, Result<CreateTenantResponse>>
{
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

        var tenant = await tenantProvisioningService.ProvisionAsync(
            userId.Value, request.Name, subdomain, request.TenantType, cancellationToken, createDefaultBrandProfile: true);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<CreateTenantResponse>.Success(
            new CreateTenantResponse(tenant.Id, tenant.Name, tenant.Subdomain, tenant.TenantType, tenant.SubscriptionId));
    }
}
