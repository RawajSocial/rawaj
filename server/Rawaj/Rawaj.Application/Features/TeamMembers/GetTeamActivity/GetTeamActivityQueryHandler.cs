using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.TeamMembers.GetTeamActivity;

public class GetTeamActivityQueryHandler(
    IApplicationDbContext dbContext, IIdentityService identityService, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetTeamActivityQuery, Result<TeamActivityPageResponse>>
{
    public async Task<Result<TeamActivityPageResponse>> Handle(GetTeamActivityQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var query = dbContext.AuditLogs
            .Where(l => l.TenantId == tenantId && l.Action.StartsWith("team."));

        if (request.UserId is not null)
        {
            query = query.Where(l => l.UserId == request.UserId);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new { l.Id, l.Action, l.Message, l.UserId, l.CreatedAt })
            .ToListAsync(cancellationToken);

        var userIds = logs.Where(l => l.UserId is not null).Select(l => l.UserId!.Value).Distinct();
        var users = await identityService.FindByIdsAsync(userIds, cancellationToken);
        var usersById = users.ToDictionary(u => u.Id);

        var items = logs
            .Select(l => new TeamActivitySummary(
                l.Id, l.Action, l.Message, l.UserId,
                l.UserId is not null && usersById.TryGetValue(l.UserId.Value, out var user) ? user.FullName : null,
                l.CreatedAt))
            .ToList();

        return Result<TeamActivityPageResponse>.Success(new TeamActivityPageResponse(items, totalCount));
    }
}
