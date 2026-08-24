using Rawaj.Domain.Common;

namespace Rawaj.Domain.Entities.Billing;

/// <summary>
/// Immutable audit trail for every coin movement — spends, allocations, purchases, and plan
/// grants. Complements (doesn't replace) the cached running balances on Tenant.CoinBalance and
/// TenantMember.AllocatedCoins/SpentCoins: those stay the fast-path read for "can this action
/// afford its cost" checks (summing the ledger on every AI generation would be needless overhead),
/// while this table exists for auditing, refunds, and support — reconcilable against the cached
/// balances if they're ever suspected to drift.
/// </summary>
public class CoinLedgerEntry : BaseEntity
{
    public Guid TenantId { get; set; }
    /// <summary>Null when the entry is against the tenant pool (Owner/Admin); set when it's against
    /// a specific invited member's escrowed wallet.</summary>
    public Guid? TenantMemberId { get; set; }
    /// <summary>Signed — negative for a spend, positive for a grant/purchase/allocation.</summary>
    public int Amount { get; set; }
    /// <summary>Short machine-readable reason, e.g. "content_generation", "coin_purchase",
    /// "plan_change_grant", "member_allocation".</summary>
    public string Reason { get; set; } = null!;
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public DateTime CreatedAt { get; set; }
}
