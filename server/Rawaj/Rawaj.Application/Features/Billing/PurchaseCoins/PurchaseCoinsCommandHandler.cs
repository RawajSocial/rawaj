using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Billing.PurchaseCoins;

public class PurchaseCoinsCommandHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<PurchaseCoinsCommand, Result<PurchaseCoinsResponse>>
{
    /// <summary>Custom amounts are priced at the Starter package's flat rate: $50 for 5,000 coins,
    /// no bonus. Public so <c>GetCoinPricingQueryHandler</c> can surface the same rate.</summary>
    public const decimal CustomPricePerCoin = 0.01m;

    public async Task<Result<PurchaseCoinsResponse>> Handle(PurchaseCoinsCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        int coinsGranted;
        decimal amountUsd;
        string description;

        if (request.CoinPackageId.HasValue)
        {
            var package = await dbContext.CoinPackages
                .FirstOrDefaultAsync(p => p.Id == request.CoinPackageId.Value && p.IsActive, cancellationToken);
            if (package is null)
            {
                return Result<PurchaseCoinsResponse>.Failure("Coin package not found.");
            }

            coinsGranted = package.Coins + package.BonusCoins;
            amountUsd = package.PriceUsd;
            description = $"Purchased the {package.Name} coin package ({coinsGranted} coins)";
        }
        else
        {
            coinsGranted = request.CustomCoins!.Value;
            amountUsd = coinsGranted * CustomPricePerCoin;
            description = $"Purchased {coinsGranted} coins (custom amount)";
        }

        var tenant = await dbContext.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);
        var now = DateTime.UtcNow;

        tenant.CoinBalance += coinsGranted;
        tenant.UpdatedAt = now;

        // Fake payment: no gateway call, no card validation, always succeeds. This is a placeholder
        // until a real payment integration exists (see the unused Stripe fields on Subscription).
        dbContext.BillingTransactions.Add(new BillingTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = BillingTransactionType.CoinPurchase,
            Description = description,
            AmountUsd = amountUsd,
            CoinsGranted = coinsGranted,
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<PurchaseCoinsResponse>.Success(
            new PurchaseCoinsResponse(coinsGranted, amountUsd, tenant.CoinBalance));
    }
}
