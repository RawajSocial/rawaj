using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Billing.PurchaseAddOn;

public record PurchaseAddOnResponse(AddOnType Type, decimal AmountUsd, int ExtraBrandsPurchased, int ExtraMarketeersPurchased);
