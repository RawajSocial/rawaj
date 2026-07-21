using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.UpdateTeamMember;

public record UpdateTeamMemberCommand(Guid TenantMemberId, TenantMemberRole Role, List<Guid> BrandProfileIds)
    : IRequest<Result<UpdateTeamMemberResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Admin;
}
