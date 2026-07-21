using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Brands.GetBrandProfiles;

public class GetBrandProfilesQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentTenantContext currentTenantContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetBrandProfilesQuery, Result<List<BrandProfileSummary>>>
{
    public async Task<Result<List<BrandProfileSummary>>> Handle(GetBrandProfilesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var brandsQuery = dbContext.TenantBrandProfiles.Where(b => b.TenantId == tenantId);

        // Owner/Admin manage every brand in the tenant; Editor/Viewer only see brands they were
        // explicitly invited to (TenantMemberBrandAccess).
        if (!currentTenantContext.Role!.Value.HasAtLeast(TenantMemberRole.Admin))
        {
            var userId = currentUserService.UserId!.Value;
            brandsQuery = brandsQuery.Where(b => dbContext.TenantMemberBrandAccesses.Any(
                a => a.BrandProfileId == b.Id && a.TenantMember.TenantId == tenantId && a.TenantMember.UserId == userId));
        }

        var entities = await brandsQuery
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
