using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Tenants.GetMyMemberships;

/// <param name="MyCoinBalance">
/// The CALLING member's own spendable balance — the tenant pool for Owner/Admin, or this member's
/// own (AllocatedCoins - SpentCoins) wallet otherwise. An invited Editor/Viewer must never see the
/// owner's full tenant balance through this endpoint.
/// </param>
/// <param name="AccountSetupCompleted">
/// Whether the CALLING user (not this specific tenant) finished their personal account setup —
/// identical across every row in the list since it's a per-user, not per-membership, fact. Gates
/// an invited (non-owner) member's baseline access; distinct from IsActivated, which is the
/// tenant's own business-profile completeness and only the owner can affect.
/// </param>
public record MembershipSummary(
    Guid TenantId,
    string Name,
    TenantType TenantType,
    TenantMemberRole Role,
    bool IsOwner,
    bool IsActivated,
    int MyCoinBalance,
    bool AccountSetupCompleted);
