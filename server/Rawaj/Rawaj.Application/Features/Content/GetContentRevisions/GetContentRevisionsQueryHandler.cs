using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Content.GetContentRevisions;

public class GetContentRevisionsQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetContentRevisionsQuery, Result<List<ContentRevisionSummary>>>
{
    public async Task<Result<List<ContentRevisionSummary>>> Handle(GetContentRevisionsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var belongsToTenant = await dbContext.ContentItems.AnyAsync(
            c => c.Id == request.ContentItemId && c.TenantId == tenantId, cancellationToken);
        if (!belongsToTenant)
        {
            return Result<List<ContentRevisionSummary>>.Failure("Content item not found.");
        }

        var revisions = await dbContext.ContentRevisions
            .Where(r => r.ContentItemId == request.ContentItemId)
            .OrderByDescending(r => r.RevisionNumber)
            .Select(r => new ContentRevisionSummary(
                r.Id, r.RevisionNumber, r.RevisionPrompt, r.Previous, r.Current, r.RevisedBy, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<List<ContentRevisionSummary>>.Success(revisions);
    }
}
