using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.GetCampaigns;

public class GetCampaignsQueryHandler(
    IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext, ICurrentUserService currentUserService)
    : IRequestHandler<GetCampaignsQuery, Result<PagedResult<CampaignSummary>>>
{
    public async Task<Result<PagedResult<CampaignSummary>>> Handle(GetCampaignsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var (page, pageSize) = PaginationDefaults.Clamp(request.Page, request.PageSize);

        var query = dbContext.MarketingCampaigns
            .Where(c => c.BrandProfile.TenantId == tenantId)
            .Where(c => request.BrandProfileId == null || c.BrandProfileId == request.BrandProfileId)
            .Where(c => request.IncludeArchived || c.Status != CampaignStatus.Archived);

        // No explicit brand filter and the caller is Editor/Viewer: restrict to their accessible
        // brands rather than leaking every brand's campaigns tenant-wide (same rule as
        // GetBrandProfilesQueryHandler). Owner/Admin and explicit-BrandProfileId requests are
        // already covered by the pipeline's IRequireResolvedBrandAccess check.
        if (request.BrandProfileId is null && !currentTenantContext.Role!.Value.HasAtLeast(TenantMemberRole.Admin))
        {
            var userId = currentUserService.UserId!.Value;
            query = query.Where(c => dbContext.TenantMemberBrandAccesses.Any(
                a => a.BrandProfileId == c.BrandProfileId && a.TenantMember.TenantId == tenantId && a.TenantMember.UserId == userId));
        }

        var orderedQuery = query.OrderByDescending(c => c.CreatedAt);

        var totalCount = await orderedQuery.CountAsync(cancellationToken);

        var campaigns = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CampaignSummary(
                c.Id, c.BrandProfileId, c.Name, c.Status, c.StartDate, c.EndDate, c.CreatedAt,
                c.Objective, c.TargetPlatforms, c.BudgetAmount, c.BudgetCurrency, c.PlanApprovedAt,
                c.ContentItems.Count(i => !i.IsDeleted), c.OnboardingCompletedAt))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<CampaignSummary>>.Success(new PagedResult<CampaignSummary>(campaigns, page, pageSize, totalCount));
    }
}
