namespace Rawaj.Application.Features.Billing.Common;

/// <summary>
/// Tracks the single still-open Checkout Session per (tenant, purchase intent) — see
/// <see cref="Rawaj.Domain.Entities.Billing.PendingCheckoutSession"/> for why this is what actually
/// stops a duplicate purchase (two tabs, a retry) from resulting in two payable sessions, rather
/// than just a short time-windowed idempotency key.
/// </summary>
public interface IPendingCheckoutSessionService
{
    /// <summary>Returns the still-open checkout URL for this exact tenant+intent if one exists, so
    /// the caller can skip creating a new Stripe session entirely and just redirect there again.</summary>
    Task<string?> FindOpenCheckoutUrlAsync(Guid tenantId, string intentKey, CancellationToken cancellationToken);

    /// <summary>Records (or overwrites) the open session for this tenant+intent, right after
    /// creating it with Stripe.</summary>
    Task TrackAsync(
        Guid tenantId, string intentKey, string stripeSessionId, string checkoutUrl, DateTime expiresAt,
        CancellationToken cancellationToken);

    /// <summary>Called once a session is actually fulfilled — frees the tenant+intent up for a
    /// genuinely new purchase from that point on.</summary>
    Task ClearAsync(string stripeSessionId, CancellationToken cancellationToken);
}
