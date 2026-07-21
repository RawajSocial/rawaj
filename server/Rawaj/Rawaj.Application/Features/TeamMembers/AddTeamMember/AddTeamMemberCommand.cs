using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.AddTeamMember;

public record AddTeamMemberCommand(string Email, TenantMemberRole Role, List<Guid> BrandProfileIds)
    : IRequest<Result<AddTeamMemberResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Admin;
}
