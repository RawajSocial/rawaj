using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Brands.GetBrandProfiles;

public class GetBrandProfilesQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetBrandProfilesQuery, Result<List<BrandProfileSummary>>>
{
    public async Task<Result<List<BrandProfileSummary>>> Handle(GetBrandProfilesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var entities = await dbContext.TenantBrandProfiles
            .Where(b => b.TenantId == tenantId)
            .OrderBy(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

        var brandProfiles = entities
            .Select(b => new BrandProfileSummary(
                b.Id,
                b.Name,
                b.Description,
                b.BrandVoice,
                b.Status,
                b.BrandInfo?.IsDefault ?? false))
            .ToList();

        return Result<List<BrandProfileSummary>>.Success(brandProfiles);
    }
}
