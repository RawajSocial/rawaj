using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.AddTeamMember;

public record AddTeamMemberResponse(
    Guid? TenantMemberId,
    Guid? InvitationId,
    string Email,
    TenantMemberRole Role,
    bool RequiresRegistration);
