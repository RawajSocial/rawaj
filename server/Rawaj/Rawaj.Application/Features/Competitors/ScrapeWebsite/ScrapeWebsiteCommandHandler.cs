using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.BrandIntelligence;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Competitors.ScrapeWebsite;

/// <summary>
/// Indexes one specific, caller-chosen URL (e.g. a competitor's pricing page) as opposed to
/// AnalyzeCompetitorCommand, which runs a general Tavily search and indexes whatever it returns.
/// </summary>
public class ScrapeWebsiteCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IWebScraperService webScraperService)
    : IRequestHandler<ScrapeWebsiteCommand, Result<ScrapeWebsiteResponse>>
{
    public async Task<Result<ScrapeWebsiteResponse>> Handle(ScrapeWebsiteCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;

        var competitor = await dbContext.Competitors
            .FirstOrDefaultAsync(c => c.Id == request.CompetitorId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (competitor is null)
        {
            return Result<ScrapeWebsiteResponse>.Failure("Competitor not found.");
        }

        var creditsUsage = await AiCreditsPolicy.GetUsageAsync(dbContext, tenantId, cancellationToken);
        if (!creditsUsage.HasCreditsRemaining)
        {
            return Result<ScrapeWebsiteResponse>.Failure(
                $"Your subscription plan allows {creditsUsage.MaxCreditsMonthly} AI credits per month. Upgrade for more.");
        }

        var startedAt = DateTime.UtcNow;
        var scrapeResult = await webScraperService.ScrapeAsync(request.Url, cancellationToken);

        var now = DateTime.UtcNow;

        var job = new AiJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BrandProfileId = competitor.BrandProfileId,
            TriggeredBy = userId,
            JobType = AiJobType.RagIndex,
            Status = scrapeResult.Succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,
            InputParams = JsonSerializer.Serialize(new { url = request.Url }),
            OutputRefType = "rag_document",
            ErrorMessage = scrapeResult.ErrorMessage,
            StartedAt = startedAt,
            CompletedAt = now,
            CreatedAt = now
        };
        dbContext.AiJobs.Add(job);

        if (!scrapeResult.Succeeded)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<ScrapeWebsiteResponse>.Failure(scrapeResult.ErrorMessage ?? "Scraping the page failed. Please try again.");
        }

        var ragDocument = new RagDocument
        {
            Id = Guid.NewGuid(),
            CompetitorId = competitor.Id,
            BrandProfileId = competitor.BrandProfileId,
            SourceType = RagSourceType.Website,
            SourceUrl = request.Url,
            CompetitorsData = scrapeResult.TextContent,
            IndexedAt = now,
            CreatedAt = now
        };
        dbContext.RagDocuments.Add(ragDocument);

        job.OutputRefId = ragDocument.Id;

        competitor.Ragged = true;
        competitor.LastScrapedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<ScrapeWebsiteResponse>.Success(
            new ScrapeWebsiteResponse(competitor.Id, ragDocument.Id, scrapeResult.Title, scrapeResult.TextContent!));
    }
}
