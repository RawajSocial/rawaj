using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Tenants.GetMyMemberships;

/// <param name="MyCoinBalance">
/// The CALLING member's own spendable balance — the tenant pool for Owner/Admin, or this member's
/// own (AllocatedCoins - SpentCoins) wallet otherwise. An invited Editor/Viewer must never see the
/// owner's full tenant balance through this endpoint.
/// </param>
public record MembershipSummary(
    Guid TenantId,
    string Name,
    TenantType TenantType,
    TenantMemberRole Role,
    bool IsOwner,
    bool IsActivated,
    int MyCoinBalance);
