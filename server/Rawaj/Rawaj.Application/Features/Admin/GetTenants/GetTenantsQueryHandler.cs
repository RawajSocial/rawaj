using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Admin.GetTenants;

public class GetTenantsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetTenantsQuery, Result<PagedResult<AdminTenantSummary>>>
{
    public async Task<Result<PagedResult<AdminTenantSummary>>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = PaginationDefaults.Clamp(request.Page, request.PageSize);

        var query = dbContext.Tenants.OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var tenants = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Subdomain,
                t.IsActive,
                t.OwnerUserId,
                t.CreatedAt,
                PlanName = dbContext.Subscriptions
                    .Where(s => s.Id == t.SubscriptionId)
                    .Select(s => s.SubscriptionPlan.Name)
                    .FirstOrDefault(),
                MemberCount = dbContext.TenantMembers.Count(m => m.TenantId == t.Id)
            })
            .ToListAsync(cancellationToken);

        var summaries = tenants
            .Select(t => new AdminTenantSummary(
                t.Id, t.Name, t.Subdomain, t.IsActive, t.OwnerUserId, t.PlanName ?? "Unknown", t.MemberCount, t.CreatedAt))
            .ToList();

        return Result<PagedResult<AdminTenantSummary>>.Success(new PagedResult<AdminTenantSummary>(summaries, page, pageSize, totalCount));
    }
}
