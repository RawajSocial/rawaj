using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.GetPendingInvitations;

public record PendingInvitationSummary(
    Guid InvitationId,
    string Email,
    TenantMemberRole Role,
    int AllocatedCoins,
    DateTime CreatedAt,
    DateTime ExpiresAt);
