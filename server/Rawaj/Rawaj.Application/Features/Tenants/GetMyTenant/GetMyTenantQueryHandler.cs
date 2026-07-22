using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

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

        var row = await (
            from member in dbContext.TenantMembers
            join tenant in dbContext.Tenants on member.TenantId equals tenant.Id
            join subscription in dbContext.Subscriptions on tenant.SubscriptionId equals subscription.Id
            join plan in dbContext.SubscriptionPlans on subscription.SubscriptionPlanId equals plan.Id
            where member.UserId == userId && member.InvitationStatus == InvitationStatus.Accepted
            select new
            {
                tenant.Id,
                tenant.Name,
                tenant.Subdomain,
                tenant.TenantType,
                member.Role,
                tenant.IsActive,
                tenant.CoinBalance,
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

        var response = new GetMyTenantResponse(
            row.Id,
            row.Name,
            row.Subdomain,
            row.TenantType,
            row.Role,
            row.IsActive,
            row.CoinBalance,
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
