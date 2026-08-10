using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Billing.Webhooks;

/// <summary>
/// The "verify on return" fallback for environments with no Stripe webhook configured (e.g. local
/// development without the Stripe CLI running): called by the frontend right after the browser
/// returns from Checkout, using the <c>session_id</c> Stripe appended to the success URL. Fetches
/// the session directly from Stripe and, if it's genuinely confirmed paid/complete, fulfills it —
/// reusing the exact same logic <c>HandleStripeWebhookEventCommand</c> uses, so there's exactly one
/// fulfillment code path regardless of which of the two triggers it.
///
/// This is a fallback, not a replacement: a user who pays and closes the tab before this call fires
/// won't be caught by it — only a real webhook guarantees fulfillment in that case. Safe to call
/// repeatedly; returns <c>false</c> (not a failure) if the session isn't confirmed yet.
/// </summary>
public record VerifyCheckoutSessionCommand(string SessionId) : IRequest<Result<bool>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;
}
