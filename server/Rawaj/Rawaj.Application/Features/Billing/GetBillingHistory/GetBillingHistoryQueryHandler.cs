using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Billing.GetBillingHistory;

public class GetBillingHistoryQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetBillingHistoryQuery, Result<GetBillingHistoryResponse>>
{
    public async Task<Result<GetBillingHistoryResponse>> Handle(GetBillingHistoryQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var query = dbContext.BillingTransactions.Where(t => t.TenantId == tenantId);

        var totalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new BillingTransactionSummary(t.Id, t.Type, t.Description, t.AmountUsd, t.CoinsGranted, t.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<GetBillingHistoryResponse>.Success(new GetBillingHistoryResponse(items, totalCount));
    }
}
