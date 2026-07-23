using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.GetInvitationDetails;

public record InvitationDetailsResponse(
    string TenantName,
    string InviterName,
    string Email,
    TenantMemberRole Role,
    bool RequiresRegistration,
    Guid? TenantMemberId,
    Guid? InvitationId);
