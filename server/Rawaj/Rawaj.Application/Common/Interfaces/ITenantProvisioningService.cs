using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Interfaces;

public interface ITenantProvisioningService
{
    Task<Guid> ProvisionPrivateTenantAsync(Guid ownerUserId, string ownerUserName, CancellationToken cancellationToken);

    Task<InviteMemberResult> InviteMemberAsync(Guid tenantId, string email, TenantMemberRole role, Guid invitedByUserId, CancellationToken cancellationToken);

    // Deliberately not unified in Phase 1: {invitationId} accepts either a TenantInvitation.Id
    // (invitee has no account yet) or a TenantMember.Id (invitee already has an account and was
    // added directly as a pending member). Both lookups are tried below. See G2 in the roadmap.
    Task<InvitationDetailsDto?> GetInvitationAsync(Guid invitationId, CancellationToken cancellationToken);

    Task<InvitationActionOutcome> AcceptInvitationAsync(Guid invitationId, Guid currentUserId, string currentUserEmail, CancellationToken cancellationToken);

    Task<InvitationActionOutcome> DeclineInvitationAsync(Guid invitationId, Guid currentUserId, string currentUserEmail, CancellationToken cancellationToken);
}
