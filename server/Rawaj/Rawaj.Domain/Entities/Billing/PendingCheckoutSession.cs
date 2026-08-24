using Rawaj.Domain.Common;
using Rawaj.Domain.Entities.Tenants;

namespace Rawaj.Domain.Entities.Billing;

/// <summary>
/// Tracks the single still-open (unpaid, unexpired) Stripe Checkout Session for a given tenant +
/// purchase intent, so that re-requesting the same purchase — a double-clicked buy button that got
/// past the frontend's disable guard, a user backing out and retrying, two open tabs — reuses the
/// existing session instead of minting a new one. Since Stripe Checkout is single-use once paid,
/// reusing the same session for both tabs is what actually makes a duplicate charge from two tabs
/// impossible, not just unlikely.
///
/// One row per (TenantId, IntentKey) — see <see cref="Common.CheckoutIntentKey"/> for how that key
/// is built. Deleted once the session is fulfilled (see
/// <c>HandleStripeWebhookEventCommandHandler</c>), freeing the tenant to start a genuinely new
/// purchase of the same thing.
/// </summary>
public class PendingCheckoutSession : BaseEntity
{
    public Guid TenantId { get; set; }
    public string IntentKey { get; set; } = null!;
    public string StripeSessionId { get; set; } = null!;
    public string CheckoutUrl { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
