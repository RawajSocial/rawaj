using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Billing.Common;

namespace Rawaj.Application.Features.Billing.PurchaseCoins;

public class CreatePurchaseCoinsCheckoutSessionCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentTenantContext currentTenantContext,
    IPaymentGatewayService paymentGateway,
    IPendingCheckoutSessionService pendingCheckoutSessionService)
    : IRequestHandler<CreatePurchaseCoinsCheckoutSessionCommand, Result<CreateCheckoutSessionResponse>>
{
    public async Task<Result<CreateCheckoutSessionResponse>> Handle(
        CreatePurchaseCoinsCheckoutSessionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var intentKey = request.CoinPackageId.HasValue
            ? CheckoutIntentKey.ForCoinPackage(request.CoinPackageId.Value)
            : CheckoutIntentKey.ForCustomCoins(request.CustomCoins!.Value);

        // A still-open session for this exact purchase already exists — reuse it instead of minting
        // a new one, so a double-click or a second open tab lands on the same Stripe payment page
        // rather than a fresh, independently payable one.
        var openCheckoutUrl = await pendingCheckoutSessionService.FindOpenCheckoutUrlAsync(tenantId, intentKey, cancellationToken);
        if (openCheckoutUrl is not null)
        {
            return Result<CreateCheckoutSessionResponse>.Success(new CreateCheckoutSessionResponse(openCheckoutUrl));
        }

        string productName;
        decimal amountUsd;
        var metadata = new Dictionary<string, string>
        {
            ["tenantId"] = tenantId.ToString(),
            ["flow"] = "CoinPurchase",
        };

        if (request.CoinPackageId.HasValue)
        {
            var package = await dbContext.CoinPackages
                .FirstOrDefaultAsync(p => p.Id == request.CoinPackageId.Value && p.IsActive, cancellationToken);
            if (package is null)
            {
                return Result<CreateCheckoutSessionResponse>.Failure("Coin package not found.");
            }

            productName = $"{package.Name} coin package ({package.Coins + package.BonusCoins} coins)";
            amountUsd = package.PriceUsd;
            metadata["coinPackageId"] = package.Id.ToString();
        }
        else
        {
            var coins = request.CustomCoins!.Value;
            productName = $"{coins} coins";
            amountUsd = coins * BillingPricing.CustomCoinPricePerCoin;
            metadata["customCoins"] = coins.ToString();
        }

        var result = await paymentGateway.CreateOneOffCheckoutSessionAsync(
            new OneOffCheckoutSessionRequest(tenantId, productName, amountUsd, metadata), cancellationToken);

        await pendingCheckoutSessionService.TrackAsync(
            tenantId, intentKey, result.SessionId, result.CheckoutUrl, result.ExpiresAt, cancellationToken);

        return Result<CreateCheckoutSessionResponse>.Success(new CreateCheckoutSessionResponse(result.CheckoutUrl));
    }
}
