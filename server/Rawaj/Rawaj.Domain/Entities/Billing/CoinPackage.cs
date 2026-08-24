using Rawaj.Domain.Common;

namespace Rawaj.Domain.Entities.Billing;

/// <summary>A fixed, purchasable coin bundle (Starter/Growth/Business/Enterprise). The "Custom"
/// package from the pricing sheet has no row here — it's a dynamic amount priced at this table's
/// flat per-coin rate, handled entirely in <c>PurchaseCoinsCommandHandler</c>.</summary>
public class CoinPackage : BaseEntity
{
    public string Name { get; set; } = null!;
    public int Coins { get; set; }
    public int BonusCoins { get; set; }
    public decimal PriceUsd { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
