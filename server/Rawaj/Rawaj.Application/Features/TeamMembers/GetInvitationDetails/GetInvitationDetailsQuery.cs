using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.TeamMembers.GetInvitationDetails;

/// <summary>
/// Anonymous lookup for the `/invite?token=...` page. The token is either a
/// <see cref="Rawaj.Domain.Entities.Tenants.TenantMember"/> id (existing-account invite — the
/// email link doubles as a capability token since the accept/decline endpoints already require
/// the caller to be authenticated as that exact invited user) or a hashed
/// <see cref="Rawaj.Domain.Entities.Tenants.TenantInvitation"/> token (brand-new email, no account yet).
/// </summary>
public record GetInvitationDetailsQuery(string Token) : IRequest<Result<InvitationDetailsResponse>>;
