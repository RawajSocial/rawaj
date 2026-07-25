using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.GetInvitationDetails;

// Owner/Admin invites aren't scoped to specific brands (they get full tenant access), so
// BrandProfiles is only populated for brand-scoped roles (Editor/Viewer) — the frontend shows
// "كل العلامات التجارية" instead when FullTenantAccess is true.
public record InvitationDetailsResponse(
    string TenantName,
    string InviterName,
    string Email,
    TenantMemberRole Role,
    bool RequiresRegistration,
    Guid? TenantMemberId,
    Guid? InvitationId,
    bool FullTenantAccess,
    List<InvitationBrandSummary> BrandProfiles);
