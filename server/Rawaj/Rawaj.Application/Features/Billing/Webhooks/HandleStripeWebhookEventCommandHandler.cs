using MediatR;
using System.Linq;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Billing.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Billing.Webhooks;

/// <summary>
/// Fulfillment only ever happens here (or from the synchronous free-plan path in
/// <c>ChangeSubscriptionPlanCommandHandler</c>) — never from the browser's return to the success
/// URL, since only Stripe confirming the event proves the charge went through.
///
/// <c>checkout.session.completed</c> finalizes one-off coin/add-on purchases entirely and, for
/// subscriptions, creates the <c>Subscription</c>'s Stripe linkage and applies the first period.
/// <c>invoice.paid</c> handles every renewal after that (its first delivery, for
/// <c>billing_reason=subscription_create</c>, is redundant with <c>checkout.session.completed</c>
/// and is skipped to avoid double-granting). Every fulfillment call recorded in a
/// <c>BillingTransaction</c> is idempotency-checked against <see cref="PaymentWebhookEvent.EventId"/>
/// first, since Stripe delivers webhooks at-least-once.
/// </summary>
public class HandleStripeWebhookEventCommandHandler(
    IBillingFulfillmentService fulfillmentService, IPendingCheckoutSessionService pendingCheckoutSessionService)
    : IRequestHandler<HandleStripeWebhookEventCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(HandleStripeWebhookEventCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;

        switch (evt.EventType)
        {
            case "checkout.session.completed":
                await HandleCheckoutSessionCompletedAsync(evt, cancellationToken);
                break;
            case "invoice.paid":
                await HandleInvoicePaidAsync(evt, cancellationToken);
                break;
            case "customer.subscription.updated":
                await HandleSubscriptionUpdatedAsync(evt, cancellationToken);
                break;
            case "customer.subscription.deleted":
                if (evt.StripeSubscriptionId is not null)
                {
                    await fulfillmentService.CancelSubscriptionAsync(evt.StripeSubscriptionId, cancellationToken);
                }
                break;
            case "invoice.payment_failed":
                if (evt.StripeSubscriptionId is not null)
                {
                    await fulfillmentService.UpdateSubscriptionStatusAsync(
                        evt.StripeSubscriptionId, SubscriptionStatus.PastDue, cancellationToken);
                }
                break;
            // Any other event type is intentionally ignored — still reported as handled so Stripe
            // stops retrying it.
        }

        return Result<bool>.Success(true);
    }

    private async Task HandleCheckoutSessionCompletedAsync(PaymentWebhookEvent evt, CancellationToken cancellationToken)
    {
        // Keyed by session id, not event id: the same session can be confirmed via the real Stripe
        // webhook AND the "verify on return" fallback (VerifyCheckoutSessionCommand), each carrying
        // a different event id for the same underlying purchase.
        if (evt.CheckoutSessionId is null) return;
        if (await fulfillmentService.IsSessionAlreadyProcessedAsync(evt.CheckoutSessionId, cancellationToken)) return;

        // The session is confirmed complete either way from here — Stripe won't let it be paid
        // again regardless of what happens below, so it's no longer "open" for reuse.
        await pendingCheckoutSessionService.ClearAsync(evt.CheckoutSessionId, cancellationToken);

        if (!evt.Metadata.TryGetValue("flow", out var flow)
            || !evt.Metadata.TryGetValue("tenantId", out var tenantIdRaw)
            || !Guid.TryParse(tenantIdRaw, out var tenantId))
        {
            return;
        }

        switch (flow)
        {
            case "CoinPurchase":
                Guid? coinPackageId = evt.Metadata.TryGetValue("coinPackageId", out var pkg) && Guid.TryParse(pkg, out var pkgId)
                    ? pkgId : null;
                int? customCoins = evt.Metadata.TryGetValue("customCoins", out var custom) && int.TryParse(custom, out var customVal)
                    ? customVal : null;

                await fulfillmentService.FulfillCoinPurchaseAsync(
                    tenantId, coinPackageId, customCoins, evt.CheckoutSessionId, evt.EventId, cancellationToken);
                break;

            case "AddOnPurchase":
                if (evt.Metadata.TryGetValue("addOnType", out var addOnRaw) && Enum.TryParse<AddOnType>(addOnRaw, out var addOnType))
                {
                    await fulfillmentService.FulfillAddOnPurchaseAsync(
                        tenantId, addOnType, evt.CheckoutSessionId, evt.EventId, cancellationToken);
                }
                break;

            case "PlanChange":
                if (evt.Metadata.TryGetValue("subscriptionPlanId", out var planRaw) && Guid.TryParse(planRaw, out var planId))
                {
                    evt.Metadata.TryGetValue("agencySize", out var agencySize);
                    var servicesOffered = evt.Metadata.TryGetValue("servicesOffered", out var services)
                        ? services.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                        : null;

                    await fulfillmentService.FulfillPlanChangeAsync(
                        tenantId, planId, agencySize, servicesOffered,
                        evt.StripeCustomerId, evt.StripeSubscriptionId,
                        evt.CheckoutSessionId, evt.EventId, cancellationToken);
                }
                break;
        }
    }

    private async Task HandleInvoicePaidAsync(PaymentWebhookEvent evt, CancellationToken cancellationToken)
    {
        if (evt.StripeSubscriptionId is null) return;
        // The first invoice of a new subscription is already fulfilled by checkout.session.completed.
        if (evt.BillingReason == "subscription_create") return;
        if (await fulfillmentService.IsEventAlreadyProcessedAsync(evt.EventId, cancellationToken)) return;

        await fulfillmentService.GrantSubscriptionRenewalAsync(evt.StripeSubscriptionId, evt.EventId, cancellationToken);
    }

    private async Task HandleSubscriptionUpdatedAsync(PaymentWebhookEvent evt, CancellationToken cancellationToken)
    {
        if (evt.StripeSubscriptionId is null || evt.Status is null) return;

        SubscriptionStatus? status = evt.Status switch
        {
            "active" => SubscriptionStatus.Active,
            "trialing" => SubscriptionStatus.Trialing,
            "past_due" or "unpaid" or "incomplete" => SubscriptionStatus.PastDue,
            "canceled" => SubscriptionStatus.Cancelled,
            _ => null,
        };
        if (status is null) return;

        await fulfillmentService.UpdateSubscriptionStatusAsync(evt.StripeSubscriptionId, status.Value, cancellationToken);
    }
}
