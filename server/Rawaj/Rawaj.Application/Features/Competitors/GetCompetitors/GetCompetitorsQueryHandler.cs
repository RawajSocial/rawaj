using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Competitors.GetCompetitors;

public class GetCompetitorsQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetCompetitorsQuery, Result<PagedResult<CompetitorSummary>>>
{
    public async Task<Result<PagedResult<CompetitorSummary>>> Handle(GetCompetitorsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var (page, pageSize) = PaginationDefaults.Clamp(request.Page, request.PageSize);

        var brandProfileBelongsToTenant = await dbContext.TenantBrandProfiles
            .AnyAsync(b => b.Id == request.BrandProfileId && b.TenantId == tenantId, cancellationToken);
        if (!brandProfileBelongsToTenant)
        {
            return Result<PagedResult<CompetitorSummary>>.Failure("Brand profile not found.");
        }

        var query = dbContext.Competitors
            .Where(c => c.BrandProfileId == request.BrandProfileId)
            .OrderBy(c => c.Name);

        var totalCount = await query.CountAsync(cancellationToken);

        var competitors = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CompetitorSummary(c.Id, c.Name, c.Url, c.Ragged, c.Status, c.LastScrapedAt))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<CompetitorSummary>>.Success(new PagedResult<CompetitorSummary>(competitors, page, pageSize, totalCount));
    }
}
