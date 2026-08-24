using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Billing.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Billing.PurchaseAddOn;

public class CreatePurchaseAddOnCheckoutSessionCommandHandler(
    ICurrentTenantContext currentTenantContext,
    IPaymentGatewayService paymentGateway,
    IPendingCheckoutSessionService pendingCheckoutSessionService)
    : IRequestHandler<CreatePurchaseAddOnCheckoutSessionCommand, Result<CreateCheckoutSessionResponse>>
{
    public async Task<Result<CreateCheckoutSessionResponse>> Handle(
        CreatePurchaseAddOnCheckoutSessionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var intentKey = CheckoutIntentKey.ForAddOn(request.Type);

        var openCheckoutUrl = await pendingCheckoutSessionService.FindOpenCheckoutUrlAsync(tenantId, intentKey, cancellationToken);
        if (openCheckoutUrl is not null)
        {
            return Result<CreateCheckoutSessionResponse>.Success(new CreateCheckoutSessionResponse(openCheckoutUrl));
        }

        var (productName, amountUsd) = request.Type == AddOnType.ExtraBrand
            ? ("Extra brand slot", BillingPricing.ExtraBrandPriceUsd)
            : ("Extra marketeer seat", BillingPricing.ExtraMarketeerPriceUsd);

        var metadata = new Dictionary<string, string>
        {
            ["tenantId"] = tenantId.ToString(),
            ["flow"] = "AddOnPurchase",
            ["addOnType"] = request.Type.ToString(),
        };

        var result = await paymentGateway.CreateOneOffCheckoutSessionAsync(
            new OneOffCheckoutSessionRequest(tenantId, productName, amountUsd, metadata), cancellationToken);

        await pendingCheckoutSessionService.TrackAsync(
            tenantId, intentKey, result.SessionId, result.CheckoutUrl, result.ExpiresAt, cancellationToken);

        return Result<CreateCheckoutSessionResponse>.Success(new CreateCheckoutSessionResponse(result.CheckoutUrl));
    }
}
