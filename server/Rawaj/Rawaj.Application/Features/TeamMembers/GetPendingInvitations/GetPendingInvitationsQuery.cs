using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.GetPendingInvitations;

/// <summary>
/// Invites sent to an email with no Rawaj account yet — kept separate from
/// <c>GetTeamMembersQuery</c> since these rows have no <c>UserId</c>/profile to join against.
/// </summary>
public record GetPendingInvitationsQuery : IRequest<Result<List<PendingInvitationSummary>>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;
}
