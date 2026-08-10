using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Common.Interfaces;

/// <summary>
/// Gateway-agnostic seam over Stripe Checkout — Application code never references the Stripe.net
/// SDK directly, matching <see cref="IMediaStorageService"/>'s seam over Cloudinary.
/// </summary>
public interface IPaymentGatewayService
{
    bool IsConfigured { get; }

    Task<CheckoutSessionResult> CreateOneOffCheckoutSessionAsync(
        OneOffCheckoutSessionRequest request, CancellationToken cancellationToken);

    Task<CheckoutSessionResult> CreateSubscriptionCheckoutSessionAsync(
        SubscriptionCheckoutSessionRequest request, CancellationToken cancellationToken);

    /// <summary>The "verify on return" fallback for environments with no webhook configured: fetches
    /// the Checkout Session directly and returns a synthetic <see cref="PaymentWebhookEvent"/> if
    /// Stripe confirms it's actually paid/complete, or null if it isn't (yet). Doesn't require
    /// <c>Stripe:WebhookSecret</c> to be set.</summary>
    Task<PaymentWebhookEvent?> RetrieveCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken);

    /// <summary>Verifies the Stripe-Signature header against the raw payload and parses the event.
    /// Throws if the signature is invalid — callers should let that surface as a 400.</summary>
    PaymentWebhookEvent ParseAndVerifyWebhookEvent(string payload, string signatureHeader);
}
