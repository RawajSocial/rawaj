using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Tenants.GetTenantUsageSummary;

/// <summary>
/// Compute-on-read aggregates over existing tables for a lightweight usage dashboard — not a
/// pre-aggregated rollup table. Revisit with a materialized summary only if a specific dashboard
/// query proves slow under real load.
/// </summary>
public class GetTenantUsageSummaryQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetTenantUsageSummaryQuery, Result<TenantUsageSummaryResponse>>
{
    public async Task<Result<TenantUsageSummaryResponse>> Handle(GetTenantUsageSummaryQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var contentItemsGenerated = await dbContext.ContentItems
            .CountAsync(c => c.TenantId == tenantId, cancellationToken);

        var visualAssetsGenerated = await dbContext.VisualAssets
            .CountAsync(a => a.TenantId == tenantId, cancellationToken);

        var campaignsCreated = await dbContext.MarketingCampaigns
            .CountAsync(c => c.BrandProfile.TenantId == tenantId, cancellationToken);

        var postsPublished = await dbContext.ScheduledPosts
            .CountAsync(p => p.BrandProfile.TenantId == tenantId && p.Status == Domain.Enums.ScheduledPostStatus.Published, cancellationToken);

        var coinsSpentAllTime = -await dbContext.CoinLedgerEntries
            .Where(e => e.TenantId == tenantId && e.Amount < 0)
            .SumAsync(e => (int?)e.Amount, cancellationToken) ?? 0;

        var coinsSpentLast30Days = -await dbContext.CoinLedgerEntries
            .Where(e => e.TenantId == tenantId && e.Amount < 0 && e.CreatedAt >= thirtyDaysAgo)
            .SumAsync(e => (int?)e.Amount, cancellationToken) ?? 0;

        return Result<TenantUsageSummaryResponse>.Success(new TenantUsageSummaryResponse(
            contentItemsGenerated,
            visualAssetsGenerated,
            campaignsCreated,
            postsPublished,
            coinsSpentAllTime,
            coinsSpentLast30Days));
    }
}
