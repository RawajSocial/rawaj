using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.GetMyPendingInvites;

public record PendingInviteSummary(
    Guid TenantMemberId,
    Guid TenantId,
    string TenantName,
    TenantMemberRole Role,
    List<string> BrandProfileNames,
    DateTime CreatedAt);
