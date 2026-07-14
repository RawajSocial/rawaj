using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Interfaces;

public interface ITenantProvisioningService
{
    Task<Guid> ProvisionPrivateTenantAsync(Guid ownerUserId, string ownerUserName, CancellationToken cancellationToken);

    Task InviteMemberAsync(Guid tenantId, string email, TenantMemberRole role, Guid invitedByUserId, CancellationToken cancellationToken);

    Task<InvitationDetailsDto?> GetInvitationAsync(Guid invitationId, CancellationToken cancellationToken);

    Task<InvitationActionOutcome> AcceptInvitationAsync(Guid invitationId, Guid currentUserId, string currentUserEmail, CancellationToken cancellationToken);

    Task<InvitationActionOutcome> DeclineInvitationAsync(Guid invitationId, Guid currentUserId, string currentUserEmail, CancellationToken cancellationToken);
}
