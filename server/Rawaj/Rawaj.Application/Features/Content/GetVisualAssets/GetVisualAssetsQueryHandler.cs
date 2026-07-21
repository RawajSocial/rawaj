using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Content.GetVisualAssets;

public class GetVisualAssetsQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetVisualAssetsQuery, Result<PagedResult<VisualAssetSummary>>>
{
    public async Task<Result<PagedResult<VisualAssetSummary>>> Handle(GetVisualAssetsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var (page, pageSize) = PaginationDefaults.Clamp(request.Page, request.PageSize);

        var campaignBelongsToTenant = await dbContext.MarketingCampaigns
            .AnyAsync(c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (!campaignBelongsToTenant)
        {
            return Result<PagedResult<VisualAssetSummary>>.Failure("Campaign not found.");
        }

        var query = dbContext.VisualAssets
            .Where(a => a.CampaignId == request.CampaignId)
            .OrderByDescending(a => a.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var assets = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new VisualAssetSummary(a.Id, a.ContentItemId, a.Type, a.FileUrl, a.IsApproved, a.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<VisualAssetSummary>>.Success(new PagedResult<VisualAssetSummary>(assets, page, pageSize, totalCount));
    }
}
