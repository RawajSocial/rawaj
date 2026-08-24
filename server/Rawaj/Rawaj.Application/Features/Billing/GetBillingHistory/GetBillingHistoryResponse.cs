using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Billing.GetBillingHistory;

public record BillingTransactionSummary(
    Guid Id, BillingTransactionType Type, string Description, decimal AmountUsd, int? CoinsGranted, DateTime CreatedAt);

public record GetBillingHistoryResponse(List<BillingTransactionSummary> Items, int TotalCount);
