using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Billing.PurchaseCoins;

/// <summary>
/// Buys a fixed coin package (<paramref name="CoinPackageId"/>) or a custom amount
/// (<paramref name="CustomCoins"/>, priced at the Starter package's flat $0.01/coin rate, no
/// bonus) — exactly one of the two must be provided. Fake payment: no gateway call, always
/// succeeds, credited immediately.
/// </summary>
public record PurchaseCoinsCommand(Guid? CoinPackageId, int? CustomCoins)
    : IRequest<Result<PurchaseCoinsResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Admin;
}
