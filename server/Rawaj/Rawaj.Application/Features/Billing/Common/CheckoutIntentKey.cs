using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Billing.Common;

/// <summary>Builds a stable identifier for "what is being bought", scoped per tenant by
/// <see cref="IPendingCheckoutSessionService"/> — two requests for the exact same intent reuse the
/// same open Checkout Session; two different intents (a different package, a different plan) never
/// collide with each other.</summary>
public static class CheckoutIntentKey
{
    public static string ForCoinPackage(Guid coinPackageId) => $"coins:package:{coinPackageId}";

    public static string ForCustomCoins(int coins) => $"coins:custom:{coins}";

    public static string ForAddOn(AddOnType type) => $"addon:{type}";

    public static string ForPlanChange(Guid subscriptionPlanId) => $"plan:{subscriptionPlanId}";
}
