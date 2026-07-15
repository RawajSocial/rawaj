using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.GetTeamMembers;

public record TeamMemberSummary(
    Guid TenantMemberId,
    Guid UserId,
    string Email,
    string FullName,
    TenantMemberRole Role,
    DateTime? JoinedAt);
