using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.GetScheduledPosts;

public class GetScheduledPostsQueryHandler(
    IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext, ICurrentUserService currentUserService)
    : IRequestHandler<GetScheduledPostsQuery, Result<PagedResult<ScheduledPostSummary>>>
{
    public async Task<Result<PagedResult<ScheduledPostSummary>>> Handle(GetScheduledPostsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var (page, pageSize) = PaginationDefaults.Clamp(request.Page, request.PageSize);

        var query = dbContext.ScheduledPosts
            .Where(s => s.ContentItem.TenantId == tenantId)
            .Where(s => request.CampaignId == null || s.ContentItem.CampaignId == request.CampaignId);

        // No explicit campaign filter and the caller is Editor/Viewer: restrict to scheduled posts
        // on brands they're accessible for, rather than leaking every brand's schedule tenant-wide.
        if (request.CampaignId is null && !currentTenantContext.Role!.Value.HasAtLeast(TenantMemberRole.Admin))
        {
            var userId = currentUserService.UserId!.Value;
            query = query.Where(s => dbContext.TenantMemberBrandAccesses.Any(
                a => a.BrandProfileId == s.SocialAccount.BrandProfileId && a.TenantMember.TenantId == tenantId && a.TenantMember.UserId == userId));
        }

        var orderedQuery = query.OrderBy(s => s.ScheduledAt);

        var totalCount = await orderedQuery.CountAsync(cancellationToken);

        var posts = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ScheduledPostSummary(
                s.Id,
                s.ContentItemId,
                s.SocialAccount.Platform,
                s.SocialAccount.AccountName,
                s.ScheduledAt,
                s.Status,
                s.PublishedAt,
                s.ErrorMessage))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<ScheduledPostSummary>>.Success(new PagedResult<ScheduledPostSummary>(posts, page, pageSize, totalCount));
    }
}
