using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Content.GetContentItems;

public class GetContentItemsQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetContentItemsQuery, Result<PagedResult<ContentItemSummary>>>
{
    public async Task<Result<PagedResult<ContentItemSummary>>> Handle(GetContentItemsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var (page, pageSize) = PaginationDefaults.Clamp(request.Page, request.PageSize);

        var query = dbContext.ContentItems
            .Where(c => c.CampaignId == request.CampaignId && c.TenantId == tenantId)
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ContentItemSummary(c.Id, c.ContentType, c.Platform, c.Language, c.Content, c.Status, c.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<ContentItemSummary>>.Success(new PagedResult<ContentItemSummary>(items, page, pageSize, totalCount));
    }
}
