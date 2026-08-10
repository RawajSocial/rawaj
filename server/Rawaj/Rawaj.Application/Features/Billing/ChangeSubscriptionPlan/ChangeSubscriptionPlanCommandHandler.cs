using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Billing.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Billing.ChangeSubscriptionPlan;

/// <summary>
/// The one "subscribe/upgrade/downgrade" endpoint. The Free plan (<c>Cost == 0</c>) applies
/// synchronously — there's nothing to charge, so it goes straight to
/// <see cref="IBillingFulfillmentService.FulfillPlanChangeAsync"/>. Any paid plan instead starts a
/// Stripe Checkout subscription and returns its URL; the actual plan swap, coin grant, and — the
/// first time a tenant leaves the Free plan — the flip to <see cref="TenantType.Agency"/> all happen
/// from the webhook once Stripe confirms the first payment.
/// <paramref name="AgencySize"/>/<paramref name="ServicesOffered"/> are only required for that first
/// paid subscription; omit them on any later plan change.
/// </summary>
public class ChangeSubscriptionPlanCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentTenantContext currentTenantContext,
    IIdentityService identityService,
    IPaymentGatewayService paymentGateway,
    IBillingFulfillmentService fulfillmentService,
    IPendingCheckoutSessionService pendingCheckoutSessionService)
    : IRequestHandler<ChangeSubscriptionPlanCommand, Result<ChangeSubscriptionPlanResponse>>
{
    public async Task<Result<ChangeSubscriptionPlanResponse>> Handle(
        ChangeSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var plan = await dbContext.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == request.SubscriptionPlanId && p.IsActive, cancellationToken);
        if (plan is null)
        {
            return Result<ChangeSubscriptionPlanResponse>.Failure("Subscription plan not found.");
        }

        if (plan.Cost <= 0)
        {
            var response = await fulfillmentService.FulfillPlanChangeAsync(
                tenantId, plan.Id, request.AgencySize, request.ServicesOffered,
                stripeCustomerId: null, stripeSubscriptionId: null,
                stripeSessionId: null, stripeEventId: null, cancellationToken);

            return Result<ChangeSubscriptionPlanResponse>.Success(response);
        }

        var tenant = await dbContext.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);
        var subscription = await dbContext.Subscriptions.FirstAsync(s => s.Id == tenant.SubscriptionId, cancellationToken);

        // Still-current (pre-change) subscription state, matching ChangeSubscriptionPlanResponse's
        // "for a paid plan, everything except CheckoutUrl is a snapshot of what's still true today"
        // contract — used for both the reused-session case below and the newly-created one further
        // down, so it's read once here.
        var snapshot = new ChangeSubscriptionPlanResponse(
            subscription.Id, plan.Name, plan.Cost, subscription.Status,
            subscription.CurrentPeriodStart, subscription.CurrentPeriodEnd,
            tenant.TenantType, tenant.CoinBalance, CoinsGranted: 0, CheckoutUrl: null);

        var intentKey = CheckoutIntentKey.ForPlanChange(plan.Id);
        var openCheckoutUrl = await pendingCheckoutSessionService.FindOpenCheckoutUrlAsync(tenantId, intentKey, cancellationToken);
        if (openCheckoutUrl is not null)
        {
            return Result<ChangeSubscriptionPlanResponse>.Success(snapshot with { CheckoutUrl = openCheckoutUrl });
        }

        var owner = await identityService.FindByIdAsync(tenant.OwnerUserId, cancellationToken);
        if (owner is null)
        {
            return Result<ChangeSubscriptionPlanResponse>.Failure("Tenant owner not found.");
        }

        var metadata = new Dictionary<string, string>
        {
            ["tenantId"] = tenantId.ToString(),
            ["flow"] = "PlanChange",
            ["subscriptionPlanId"] = plan.Id.ToString(),
        };
        if (!string.IsNullOrWhiteSpace(request.AgencySize)) metadata["agencySize"] = request.AgencySize;
        if (request.ServicesOffered is { Count: > 0 }) metadata["servicesOffered"] = string.Join(",", request.ServicesOffered);

        var checkout = await paymentGateway.CreateSubscriptionCheckoutSessionAsync(
            new SubscriptionCheckoutSessionRequest(
                tenantId, owner.Email, subscription.StripeCustomerId, plan.Name, plan.Cost,
                plan.BillingCycle == BillingCycle.Yearly, metadata),
            cancellationToken);

        // Persist a newly-created Stripe Customer immediately — the webhook round-trip shouldn't be
        // the only place this gets saved, since a customer was just created against Stripe's API
        // regardless of whether the tenant completes checkout.
        if (subscription.StripeCustomerId != checkout.StripeCustomerId)
        {
            subscription.StripeCustomerId = checkout.StripeCustomerId;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await pendingCheckoutSessionService.TrackAsync(
            tenantId, intentKey, checkout.SessionId, checkout.CheckoutUrl, checkout.ExpiresAt, cancellationToken);

        return Result<ChangeSubscriptionPlanResponse>.Success(snapshot with { CheckoutUrl = checkout.CheckoutUrl });
    }
}
