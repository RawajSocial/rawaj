using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Billing.PurchaseAddOn;

public class PurchaseAddOnCommandHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<PurchaseAddOnCommand, Result<PurchaseAddOnResponse>>
{
    /// <summary>Public so <c>GetCoinPricingQueryHandler</c> can surface the same prices.</summary>
    public const decimal ExtraBrandPriceUsd = 5m;
    public const decimal ExtraMarketeerPriceUsd = 3m;

    public async Task<Result<PurchaseAddOnResponse>> Handle(PurchaseAddOnCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var tenant = await dbContext.Tenants.FindAsync([tenantId], cancellationToken)
            ?? throw new InvalidOperationException("Tenant not found.");

        var now = DateTime.UtcNow;
        decimal amountUsd;
        string description;

        if (request.Type == AddOnType.ExtraBrand)
        {
            tenant.ExtraBrandsPurchased++;
            amountUsd = ExtraBrandPriceUsd;
            description = "Purchased an extra brand slot";
        }
        else
        {
            tenant.ExtraMarketeersPurchased++;
            amountUsd = ExtraMarketeerPriceUsd;
            description = "Purchased an extra marketeer seat";
        }

        tenant.UpdatedAt = now;

        // Fake payment: no gateway call, no card validation, always succeeds. This is a placeholder
        // until a real payment integration exists (see the unused Stripe fields on Subscription).
        dbContext.BillingTransactions.Add(new BillingTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = BillingTransactionType.AddOnPurchase,
            Description = description,
            AmountUsd = amountUsd,
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<PurchaseAddOnResponse>.Success(
            new PurchaseAddOnResponse(request.Type, amountUsd, tenant.ExtraBrandsPurchased, tenant.ExtraMarketeersPurchased));
    }
}
