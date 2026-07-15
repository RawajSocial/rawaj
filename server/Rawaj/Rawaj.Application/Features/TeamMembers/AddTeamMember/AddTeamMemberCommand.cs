using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.AddTeamMember;

public record AddTeamMemberCommand(string Email, TenantMemberRole Role)
    : IRequest<Result<AddTeamMemberResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Admin;
}
