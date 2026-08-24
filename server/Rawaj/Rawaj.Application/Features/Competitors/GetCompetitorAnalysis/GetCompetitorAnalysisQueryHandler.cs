using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Competitors.GetCompetitorAnalysis;

public class GetCompetitorAnalysisQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetCompetitorAnalysisQuery, Result<List<RagDocumentSummary>>>
{
    public async Task<Result<List<RagDocumentSummary>>> Handle(GetCompetitorAnalysisQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var competitorBelongsToTenant = await dbContext.Competitors
            .AnyAsync(c => c.Id == request.CompetitorId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (!competitorBelongsToTenant)
        {
            return Result<List<RagDocumentSummary>>.Failure("Competitor not found.");
        }

        var documents = await dbContext.RagDocuments
            .Where(d => d.CompetitorId == request.CompetitorId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new RagDocumentSummary(d.Id, d.SourceUrl, d.CompetitorsData, d.IndexedAt))
            .ToListAsync(cancellationToken);

        return Result<List<RagDocumentSummary>>.Success(documents);
    }
}
