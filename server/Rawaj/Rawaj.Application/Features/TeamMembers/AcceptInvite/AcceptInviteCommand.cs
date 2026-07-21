using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.TeamMembers.AcceptInvite;

/// <summary>
/// Deliberately does not implement IRequireTenantRole - the invited user has no accepted membership
/// yet (that is exactly what this command grants), so TenantAuthorizationBehavior would reject it.
/// The handler verifies ownership of the invite directly instead.
/// </summary>
public record AcceptInviteCommand(Guid TenantMemberId) : IRequest<Result<bool>>;
