using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.GetTeamActivity;

public record GetTeamActivityQuery(int Page = 1, int PageSize = 20, Guid? UserId = null)
    : IRequest<Result<TeamActivityPageResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Admin;
}
