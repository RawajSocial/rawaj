using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.TeamMembers.DeclineInvite;

/// <summary>
/// Deliberately does not implement IRequireTenantRole - see AcceptInviteCommand for why.
/// </summary>
public record DeclineInviteCommand(Guid TenantMemberId) : IRequest<Result<bool>>;
