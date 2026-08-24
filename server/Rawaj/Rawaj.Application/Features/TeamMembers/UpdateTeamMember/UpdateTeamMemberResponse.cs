using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.UpdateTeamMember;

public record UpdateTeamMemberResponse(Guid TenantMemberId, TenantMemberRole Role, List<Guid> BrandProfileIds);
