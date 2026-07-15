using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.TeamMembers.GetTeamMembers;

public class GetTeamMembersQueryHandler(
    IApplicationDbContext dbContext,
    IIdentityService identityService,
    ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetTeamMembersQuery, Result<List<TeamMemberSummary>>>
{
    public async Task<Result<List<TeamMemberSummary>>> Handle(GetTeamMembersQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var members = await dbContext.TenantMembers
            .Where(m => m.TenantId == tenantId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        var users = await identityService.FindByIdsAsync(members.Select(m => m.UserId), cancellationToken);
        var usersById = users.ToDictionary(u => u.Id);

        var summaries = members
            .Where(m => usersById.ContainsKey(m.UserId))
            .Select(m =>
            {
                var user = usersById[m.UserId];
                return new TeamMemberSummary(m.Id, user.Id, user.Email, user.FullName, m.Role, m.JoinedAt);
            })
            .ToList();

        return Result<List<TeamMemberSummary>>.Success(summaries);
    }
}
