using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;

namespace Rawaj.Application.Features.Tenants.GetMyTenant;

public class GetMyTenantQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<GetMyTenantQuery, Result<GetMyTenantResponse>>
{
    public async Task<Result<GetMyTenantResponse>> Handle(GetMyTenantQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Result<GetMyTenantResponse>.Failure("Unauthorized.");
        }

        var resolved = await TenantResolutionPolicy.ResolveAsync(
            dbContext, userId.Value, currentUserService.RequestedTenantId, cancellationToken);

        if (resolved is null)
        {
            return Result<GetMyTenantResponse>.Failure("No organization found.");
        }

        var row = await (
            from tenant in dbContext.Tenants
            join subscription in dbContext.Subscriptions on tenant.SubscriptionId equals subscription.Id
            join plan in dbContext.SubscriptionPlans on subscription.SubscriptionPlanId equals plan.Id
            where tenant.Id == resolved.TenantId
            select new
            {
                tenant.Id,
                tenant.Name,
                tenant.Subdomain,
                tenant.TenantType,
                tenant.IsActive,
                tenant.IsActivated,
                PlanName = plan.Name,
                plan.MaxBrands,
                tenant.TenantProfile
            }
        ).FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return Result<GetMyTenantResponse>.Failure("No organization found.");
        }

        var brandProfileCount = await dbContext.TenantBrandProfiles
            .CountAsync(b => b.TenantId == row.Id, cancellationToken);

        var defaultBrandProfileId = await dbContext.TenantBrandProfiles
            .Where(b => b.TenantId == row.Id)
            .OrderBy(b => b.CreatedAt)
            .Select(b => (Guid?)b.Id)
            .FirstOrDefaultAsync(cancellationToken);

        // The tenant pool (row.CoinBalance) is only the caller's own balance when they're the
        // Owner/Admin — an invited Editor/Viewer must only ever see their own escrowed wallet,
        // never the owner's full pool (same rule GetMyMembershipsQueryHandler applies).
        var coinBalance = await CoinPolicy.GetBalanceAsync(dbContext, row.Id, userId.Value, resolved.Role, cancellationToken);

        var response = new GetMyTenantResponse(
            row.Id,
            row.Name,
            row.Subdomain,
            row.TenantType,
            resolved.Role,
            row.IsActive,
            coinBalance,
            row.IsActivated,
            row.PlanName,
            row.MaxBrands,
            brandProfileCount,
            defaultBrandProfileId,
            row.TenantProfile?.Phone,
            row.TenantProfile?.Industry,
            row.TenantProfile?.Country,
            row.TenantProfile?.City,
            row.TenantProfile?.Website,
            row.TenantProfile?.AgencySize,
            row.TenantProfile?.ServicesOffered ?? []);

        return Result<GetMyTenantResponse>.Success(response);
    }
}
