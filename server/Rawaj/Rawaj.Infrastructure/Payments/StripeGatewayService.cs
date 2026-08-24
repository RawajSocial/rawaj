using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Stripe;
using Stripe.Checkout;

namespace Rawaj.Infrastructure.Payments;

/// <summary>
/// The one <see cref="IPaymentGatewayService"/> implementation shipped today. Application code
/// never references the Stripe.net SDK directly — everything goes through the interface, matching
/// <c>CloudinaryMediaStorageService</c>'s seam over Cloudinary.
///
/// Every Checkout Session is priced with inline <c>price_data</c> rather than a pre-provisioned
/// Stripe Product/Price, so <c>CoinPackage.PriceUsd</c>/<c>SubscriptionPlan.Cost</c> stay the only
/// source of truth for pricing — nothing needs to be kept in sync with the Stripe Dashboard.
/// </summary>
public class StripeGatewayService : IPaymentGatewayService
{
    private readonly StripeSettings _settings;
    private readonly IFrontendUrlProvider _frontendUrlProvider;
    private readonly Lazy<StripeClient> _client;

    public StripeGatewayService(IOptions<StripeSettings> settings, IFrontendUrlProvider frontendUrlProvider)
    {
        _settings = settings.Value;
        _frontendUrlProvider = frontendUrlProvider;
        _client = new Lazy<StripeClient>(() => new StripeClient(_settings.SecretKey));
    }

    // Only the secret key is needed to create/retrieve Checkout Sessions — the webhook secret is
    // checked separately in ParseAndVerifyWebhookEvent, so the "verify on return" fallback path
    // (RetrieveCheckoutSessionAsync) works without ever configuring a webhook.
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.SecretKey) && !_settings.SecretKey.StartsWith("REPLACE_WITH_");

    public async Task<CheckoutSessionResult> CreateOneOffCheckoutSessionAsync(
        OneOffCheckoutSessionRequest request, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = ToCents(request.AmountUsd),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = request.ProductName,
                        },
                    },
                },
            ],
            Metadata = new Dictionary<string, string>(request.Metadata),
            SuccessUrl = BuildSuccessUrl(),
            CancelUrl = BuildCancelUrl(),
        };

        var requestOptions = new RequestOptions { IdempotencyKey = BuildIdempotencyKey(request.TenantId, request.Metadata) };
        var session = await new SessionService(_client.Value).CreateAsync(options, requestOptions, cancellationToken);
        return new CheckoutSessionResult(session.Id, session.Url, session.CustomerId, session.ExpiresAt);
    }

    public async Task<CheckoutSessionResult> CreateSubscriptionCheckoutSessionAsync(
        SubscriptionCheckoutSessionRequest request, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var customerId = request.ExistingStripeCustomerId;
        if (string.IsNullOrWhiteSpace(customerId))
        {
            var customer = await new CustomerService(_client.Value).CreateAsync(
                new CustomerCreateOptions { Email = request.OwnerEmail },
                cancellationToken: cancellationToken);
            customerId = customer.Id;
        }

        var metadata = new Dictionary<string, string>(request.Metadata);

        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            Customer = customerId,
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = ToCents(request.AmountUsd),
                        Recurring = new SessionLineItemPriceDataRecurringOptions
                        {
                            Interval = request.IsYearly ? "year" : "month",
                        },
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = request.PlanName,
                        },
                    },
                },
            ],
            // Checkout Session metadata isn't copied onto the Subscription/Invoice Stripe creates for
            // mode=subscription — renewal/status webhooks receive those objects, not the Session, so
            // the same metadata is set here too.
            SubscriptionData = new SessionSubscriptionDataOptions { Metadata = metadata },
            Metadata = metadata,
            SuccessUrl = BuildSuccessUrl(),
            CancelUrl = BuildCancelUrl(),
        };

        var requestOptions = new RequestOptions { IdempotencyKey = BuildIdempotencyKey(request.TenantId, metadata) };
        var session = await new SessionService(_client.Value).CreateAsync(options, requestOptions, cancellationToken);
        return new CheckoutSessionResult(session.Id, session.Url, customerId, session.ExpiresAt);
    }

    public async Task<PaymentWebhookEvent?> RetrieveCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var session = await new SessionService(_client.Value).GetAsync(sessionId, cancellationToken: cancellationToken);

        // "complete" means the Checkout flow itself finished successfully; payment_status is only
        // meaningful for mode=payment (it stays "unpaid" for a $0 line item and isn't set the same
        // way for mode=subscription), so a completed subscription session is trusted on Status alone.
        var isConfirmed = session.Status == "complete" && (session.Mode == "subscription" || session.PaymentStatus == "paid");
        if (!isConfirmed) return null;

        return new PaymentWebhookEvent(
            EventId: $"verify_{session.Id}",
            EventType: "checkout.session.completed",
            CheckoutSessionId: session.Id,
            StripeCustomerId: session.CustomerId,
            StripeSubscriptionId: session.SubscriptionId,
            BillingReason: null,
            Status: null,
            Metadata: session.Metadata ?? new Dictionary<string, string>());
    }

    public PaymentWebhookEvent ParseAndVerifyWebhookEvent(string payload, string signatureHeader)
    {
        EnsureConfigured();
        if (string.IsNullOrWhiteSpace(_settings.WebhookSecret) || _settings.WebhookSecret.StartsWith("REPLACE_WITH_"))
        {
            throw new InvalidOperationException("Stripe webhooks are not configured (Stripe:WebhookSecret).");
        }

        var stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, _settings.WebhookSecret);

        string? sessionId = null;
        string? customerId = null;
        string? subscriptionId = null;
        string? billingReason = null;
        string? status = null;
        IReadOnlyDictionary<string, string> metadata = new Dictionary<string, string>();

        switch (stripeEvent.Data.Object)
        {
            case Session session:
                sessionId = session.Id;
                customerId = session.CustomerId;
                subscriptionId = session.SubscriptionId;
                metadata = session.Metadata ?? metadata;
                break;
            case Invoice invoice:
                customerId = invoice.CustomerId;
                subscriptionId = invoice.Parent?.SubscriptionDetails?.SubscriptionId;
                billingReason = invoice.BillingReason;
                metadata = invoice.Metadata ?? metadata;
                break;
            case Subscription subscription:
                customerId = subscription.CustomerId;
                subscriptionId = subscription.Id;
                status = subscription.Status;
                metadata = subscription.Metadata ?? metadata;
                break;
        }

        return new PaymentWebhookEvent(
            stripeEvent.Id, stripeEvent.Type, sessionId, customerId, subscriptionId, billingReason, status, metadata);
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Stripe is not configured (Stripe:SecretKey/WebhookSecret) — set real test-mode credentials before accepting payments.");
        }
    }

    private static long ToCents(decimal amountUsd) => (long)Math.Round(amountUsd * 100, MidpointRounding.AwayFromZero);

    /// <summary>Deterministic per Stripe's own idempotency-key semantics: duplicate calls (a
    /// double-clicked buy button, a network retry, or even a direct duplicate API call bypassing the
    /// frontend entirely) for the same tenant buying the same thing within the same short window
    /// collapse into the single Checkout Session Stripe created for the first one, instead of each
    /// minting a separate session. A genuinely new purchase attempt outside that window gets a fresh
    /// key and is allowed through normally — this only catches accidental duplicates, not legitimate
    /// repeat purchases.</summary>
    private static string BuildIdempotencyKey(Guid tenantId, IReadOnlyDictionary<string, string> metadata)
    {
        const int bucketSeconds = 30;
        var bucket = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / bucketSeconds;
        var metadataPart = string.Join('&', metadata.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key}={kv.Value}"));
        var raw = $"{tenantId}|{metadataPart}|{bucket}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }

    private string BuildSuccessUrl() => $"{FrontendBaseUrl}/dashboard/billing?checkout=success&session_id={{CHECKOUT_SESSION_ID}}";
    private string BuildCancelUrl() => $"{FrontendBaseUrl}/dashboard/billing?checkout=cancelled";
    private string FrontendBaseUrl => _frontendUrlProvider.BaseUrl.TrimEnd('/');
}
