using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Campaigns.GetCampaigns;

public class GetCampaignsQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetCampaignsQuery, Result<PagedResult<CampaignSummary>>>
{
    public async Task<Result<PagedResult<CampaignSummary>>> Handle(GetCampaignsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var (page, pageSize) = PaginationDefaults.Clamp(request.Page, request.PageSize);

        var query = dbContext.MarketingCampaigns
            .Where(c => c.BrandProfile.TenantId == tenantId)
            .Where(c => request.BrandProfileId == null || c.BrandProfileId == request.BrandProfileId)
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var campaigns = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CampaignSummary(c.Id, c.BrandProfileId, c.Name, c.Status, c.StartDate, c.EndDate, c.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<CampaignSummary>>.Success(new PagedResult<CampaignSummary>(campaigns, page, pageSize, totalCount));
    }
}
