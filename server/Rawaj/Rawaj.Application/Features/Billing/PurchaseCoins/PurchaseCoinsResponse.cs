namespace Rawaj.Application.Features.Billing.PurchaseCoins;

public record PurchaseCoinsResponse(int CoinsGranted, decimal AmountUsd, int NewCoinBalance);
