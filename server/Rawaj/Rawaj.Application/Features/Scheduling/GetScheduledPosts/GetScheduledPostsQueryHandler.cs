using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Scheduling.GetScheduledPosts;

public class GetScheduledPostsQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetScheduledPostsQuery, Result<PagedResult<ScheduledPostSummary>>>
{
    public async Task<Result<PagedResult<ScheduledPostSummary>>> Handle(GetScheduledPostsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var (page, pageSize) = PaginationDefaults.Clamp(request.Page, request.PageSize);

        var query = dbContext.ScheduledPosts
            .Where(s => s.ContentItem.TenantId == tenantId)
            .Where(s => request.CampaignId == null || s.ContentItem.CampaignId == request.CampaignId)
            .OrderBy(s => s.ScheduledAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var posts = await query
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
