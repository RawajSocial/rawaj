using MediatR;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.AcceptInvitationAndRegister;

/// <summary>
/// The "no account yet" invite path: registers a brand-new user, provisions their own (unactivated)
/// tenant, and turns the <see cref="Rawaj.Domain.Entities.Tenants.TenantInvitation"/> into an
/// Accepted <see cref="Rawaj.Domain.Entities.Tenants.TenantMember"/> in the inviting tenant — all
/// in one call so the invitee lands logged in. Deliberately anonymous (no <c>IRequireTenantRole</c>):
/// there is no authenticated user yet.
/// </summary>
public record AcceptInvitationAndRegisterCommand(
    string Token, string Username, string Password, string FullName, Language PreferredLanguage)
    : IRequest<Result<AcceptInvitationAndRegisterResponse>>;
