using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Billing.Webhooks;

/// <summary>Dispatched by <c>StripeWebhookController</c> for every verified Stripe event. Not
/// tenant-scoped (no <c>IRequireTenantRole</c>) — Stripe calls this with no logged-in user; the
/// tenant is resolved from event metadata or a <c>Subscription.StripeSubscriptionId</c> lookup.</summary>
public record HandleStripeWebhookEventCommand(PaymentWebhookEvent Event) : IRequest<Result<bool>>;
