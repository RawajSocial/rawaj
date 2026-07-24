using Rawaj.Domain.Common;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.Billing;

/// <summary>
/// A record of money "spent" against a tenant — coin package purchases, plan changes, and add-on
/// purchases. Backs the billing page's real invoice/history list.
///
/// Every transaction here is a FAKE payment: nothing calls a real payment gateway, there's no card
/// validation, and it always succeeds. This is a placeholder until a real gateway (Stripe fields
/// already sit unused on <see cref="Subscription"/>) is integrated — at that point a webhook would
/// need to create these rows instead of the command handlers doing it synchronously and instantly.
/// </summary>
public class BillingTransaction : BaseEntity
{
    public Guid TenantId { get; set; }
    public BillingTransactionType Type { get; set; }
    public string Description { get; set; } = null!;
    public decimal AmountUsd { get; set; }
    public int? CoinsGranted { get; set; }
    public DateTime CreatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
