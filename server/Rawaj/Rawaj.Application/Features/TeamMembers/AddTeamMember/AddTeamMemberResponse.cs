using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.AddTeamMember;

public record AddTeamMemberResponse(Guid TenantMemberId, Guid UserId, string Email, TenantMemberRole Role, InvitationStatus InvitationStatus);
