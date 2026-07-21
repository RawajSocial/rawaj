using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.TeamMembers.GetMyPendingInvites;

/// <summary>
/// Deliberately does not implement IRequireTenantRole - a user with zero accepted memberships (or
/// none at all) still needs to see invites waiting on them. See AcceptInviteCommand.
/// </summary>
public record GetMyPendingInvitesQuery : IRequest<Result<List<PendingInviteSummary>>>;
