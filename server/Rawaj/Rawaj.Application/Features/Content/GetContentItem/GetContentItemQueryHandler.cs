using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Content.GetContentItem;

public class GetContentItemQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetContentItemQuery, Result<GetContentItemResponse>>
{
    public async Task<Result<GetContentItemResponse>> Handle(GetContentItemQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var response = await dbContext.ContentItems
            .Where(c => c.Id == request.ContentItemId && c.TenantId == tenantId)
            .Select(c => new GetContentItemResponse(
                c.Id,
                c.CampaignId,
                c.ContentType,
                c.Platform,
                c.Language,
                c.Title,
                c.Content,
                c.Hashtags,
                c.Cta,
                c.Tone,
                c.Status,
                c.ReviewedBy,
                c.ReviewedAt,
                c.CreatedAt,
                c.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return response is null
            ? Result<GetContentItemResponse>.Failure("Content item not found.")
            : Result<GetContentItemResponse>.Success(response);
    }
}
