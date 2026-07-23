using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Dashboard.GetDashboardActivity;

/// <summary>
/// AuditLog already carries a nullable BrandProfileId, so this is a real brand-scoped query rather
/// than an invented tracking model. NOTE: nothing in the codebase currently writes AuditLog rows
/// (no `dbContext.AuditLogs.Add(...)` call exists anywhere), so this feed will return an empty list
/// until some write path starts logging - wiring that up is a separate follow-up, out of scope
/// here. CampaignId can only be honored best-effort: AuditLog has no CampaignId column, only a
/// generic EntityType/EntityId pair, so the filter narrows to entries whose EntityType is
/// "Campaign" and EntityId matches when a CampaignId is supplied.
/// </summary>
public class GetDashboardActivityQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetDashboardActivityQuery, Result<List<DashboardActivityItem>>>
{
    public async Task<Result<List<DashboardActivityItem>>> Handle(GetDashboardActivityQuery request, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 100);

        var query = dbContext.AuditLogs.Where(a => a.BrandProfileId == request.BrandProfileId);

        if (request.CampaignId is not null)
        {
            query = query.Where(a => a.EntityType == "Campaign" && a.EntityId == request.CampaignId);
        }

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .Select(a => new DashboardActivityItem(a.Id, a.Action, a.Message, a.EntityType, a.EntityId, a.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<List<DashboardActivityItem>>.Success(items);
    }
}
