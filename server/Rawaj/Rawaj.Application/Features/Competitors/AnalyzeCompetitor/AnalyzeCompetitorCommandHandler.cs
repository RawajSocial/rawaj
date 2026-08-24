using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.BrandIntelligence;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Competitors.AnalyzeCompetitor;

public class AnalyzeCompetitorCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    ITavilySearchService tavilySearchService)
    : IRequestHandler<AnalyzeCompetitorCommand, Result<AnalyzeCompetitorResponse>>
{
    public async Task<Result<AnalyzeCompetitorResponse>> Handle(
        AnalyzeCompetitorCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;

        var competitor = await dbContext.Competitors
            .FirstOrDefaultAsync(c => c.Id == request.CompetitorId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (competitor is null)
        {
            return Result<AnalyzeCompetitorResponse>.Failure("Competitor not found.");
        }

        var brand = await dbContext.TenantBrandProfiles
            .FirstAsync(b => b.Id == competitor.BrandProfileId, cancellationToken);

        var creditsUsage = await AiCreditsPolicy.GetUsageAsync(dbContext, tenantId, cancellationToken);
        if (!creditsUsage.HasCreditsRemaining)
        {
            return Result<AnalyzeCompetitorResponse>.Failure(
                $"Your subscription plan allows {creditsUsage.MaxCreditsMonthly} AI credits per month. Upgrade for more.");
        }

        var industry = brand.BrandInfo?.Industry;
        var query = string.IsNullOrWhiteSpace(industry)
            ? $"{competitor.Name} company overview, marketing strategy, and social media presence"
            : $"{competitor.Name} company overview, marketing strategy, and social media presence in the {industry} industry";

        var startedAt = DateTime.UtcNow;
        var searchResult = await tavilySearchService.SearchAsync(query, cancellationToken);

        var now = DateTime.UtcNow;

        var job = new AiJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BrandProfileId = competitor.BrandProfileId,
            TriggeredBy = userId,
            JobType = AiJobType.MarketAnalysis,
            Status = searchResult.Succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,
            InputParams = JsonSerializer.Serialize(new { query }),
            OutputRefId = searchResult.Succeeded ? competitor.Id : null,
            OutputRefType = "competitor",
            ErrorMessage = searchResult.ErrorMessage,
            StartedAt = startedAt,
            CompletedAt = now,
            CreatedAt = now
        };
        dbContext.AiJobs.Add(job);

        if (!searchResult.Succeeded)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<AnalyzeCompetitorResponse>.Failure(
                searchResult.ErrorMessage ?? "Competitor analysis failed. Please try again.");
        }

        foreach (var item in searchResult.Results)
        {
            dbContext.RagDocuments.Add(new RagDocument
            {
                Id = Guid.NewGuid(),
                CompetitorId = competitor.Id,
                BrandProfileId = competitor.BrandProfileId,
                SourceType = RagSourceType.Website,
                SourceUrl = item.Url,
                CompetitorsData = item.Content,
                IndexedAt = now,
                CreatedAt = now
            });
        }

        competitor.Ragged = true;
        competitor.LastScrapedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        var sources = searchResult.Results.Select(r => new AnalyzeCompetitorSource(r.Title, r.Url)).ToList();

        return Result<AnalyzeCompetitorResponse>.Success(
            new AnalyzeCompetitorResponse(competitor.Id, searchResult.Answer, sources));
    }
}
