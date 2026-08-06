using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.BrandIntelligence;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.ResearchCampaignCompetitors;

/// <summary>
/// Campaign-scoped Tavily competitor research. Builds a query from the brand's industry/keywords,
/// the brief's positioning (if present), and the brand's location, then stores both a compact
/// {summary, competitors[], sources[]} blob on campaign.CompetitorResearchJson (for the campaign
/// approval UI) and one RagDocument per result (so brand-level readers benefit too, same shape
/// AnalyzeCompetitorCommandHandler already writes). Unlike that handler, this one keeps Tavily's
/// own Answer summary instead of discarding it.
///
/// This research is explicitly best-effort: per the product requirement, it "may help and may not
/// help" the eventual strategy, so a Tavily failure or empty result must never fail the campaign
/// flow — it's recorded as "unavailable" and the command still returns Result.Success, and no coins
/// are charged since no research value was actually delivered.
/// </summary>
public class ResearchCampaignCompetitorsCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    ITavilySearchService tavilySearchService,
    ICoinCostProvider coinCostProvider)
    : IRequestHandler<ResearchCampaignCompetitorsCommand, Result<ResearchCampaignCompetitorsResponse>>
{
    private const int MaxResults = 6;
    private const int SnippetMaxLength = 400;

    public async Task<Result<ResearchCampaignCompetitorsResponse>> Handle(
        ResearchCampaignCompetitorsCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;
        var role = currentTenantContext.Role!.Value;

        var campaign = await dbContext.MarketingCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (campaign is null)
        {
            return Result<ResearchCampaignCompetitorsResponse>.Failure("Campaign not found.");
        }

        var brand = await dbContext.TenantBrandProfiles.FirstAsync(b => b.Id == campaign.BrandProfileId, cancellationToken);

        var query = BuildQuery(brand, campaign);
        var startedAt = DateTime.UtcNow;
        var searchResult = await tavilySearchService.SearchAsync(query, cancellationToken);
        var now = DateTime.UtcNow;

        var job = new AiJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BrandProfileId = brand.Id,
            TriggeredBy = userId,
            JobType = AiJobType.MarketAnalysis,
            Status = searchResult.Succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,
            InputParams = JsonSerializer.Serialize(new { query }),
            OutputRefId = searchResult.Succeeded ? campaign.Id : null,
            OutputRefType = "marketing_campaign",
            ErrorMessage = searchResult.ErrorMessage,
            StartedAt = startedAt,
            CompletedAt = now,
            CreatedAt = now
        };
        dbContext.AiJobs.Add(job);

        if (!searchResult.Succeeded || searchResult.Results.Count == 0)
        {
            var note = searchResult.ErrorMessage ?? "No competitor data was found for this brand.";
            campaign.CompetitorResearchJson = JsonSerializer.Serialize(new
            {
                summary = (string?)null,
                competitors = Array.Empty<object>(),
                sources = Array.Empty<object>(),
                unavailable = true,
                note
            });
            campaign.UpdatedAt = now;

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result<ResearchCampaignCompetitorsResponse>.Success(
                new ResearchCampaignCompetitorsResponse(campaign.Id, campaign.CompetitorResearchJson, false, note, now));
        }

        var coinCost = await CoinPricingPolicy.GetDiscountedCostAsync(
            dbContext, tenantId, coinCostProvider.CompetitiveAnalysis, cancellationToken);
        var coinBalance = await CoinPolicy.GetBalanceAsync(dbContext, tenantId, userId, role, cancellationToken);
        if (coinBalance < coinCost)
        {
            return Result<ResearchCampaignCompetitorsResponse>.Failure(
                CoinPolicy.InsufficientCoinsMessage(coinCost, coinBalance, "run competitor research"));
        }

        var topResults = searchResult.Results.Take(MaxResults).ToList();

        foreach (var item in topResults)
        {
            dbContext.RagDocuments.Add(new RagDocument
            {
                Id = Guid.NewGuid(),
                BrandProfileId = brand.Id,
                SourceType = RagSourceType.Website,
                SourceUrl = item.Url,
                CompetitorsData = item.Content,
                IndexedAt = now,
                CreatedAt = now
            });
        }

        var competitors = topResults
            .Select(r => new { name = r.Title, url = r.Url, snippet = Truncate(r.Content, SnippetMaxLength) })
            .ToList();
        var sources = topResults.Select(r => new { title = r.Title, url = r.Url }).ToList();

        campaign.CompetitorResearchJson = JsonSerializer.Serialize(new
        {
            summary = searchResult.Answer,
            competitors,
            sources,
            unavailable = false,
            note = (string?)null
        });
        campaign.UpdatedAt = now;

        await CoinPolicy.TrySpendAsync(dbContext, tenantId, userId, role, coinCost, cancellationToken, reason: "competitive_analysis");

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<ResearchCampaignCompetitorsResponse>.Success(
            new ResearchCampaignCompetitorsResponse(campaign.Id, campaign.CompetitorResearchJson, true, null, now));
    }

    private static string BuildQuery(TenantBrandProfile brand, MarketingCampaign campaign)
    {
        var parts = new List<string> { $"{brand.Name} main competitors and market landscape" };

        if (!string.IsNullOrWhiteSpace(brand.BrandInfo?.Industry))
        {
            parts.Add($"in the {brand.BrandInfo.Industry} industry");
        }

        if (brand.BrandInfo?.Keywords is { Count: > 0 })
        {
            parts.Add($"related to {string.Join(", ", brand.BrandInfo.Keywords)}");
        }

        var positioning = TryExtractBriefField(campaign.BriefJson, "positioning");
        if (!string.IsNullOrWhiteSpace(positioning))
        {
            parts.Add($"positioned as {positioning}");
        }

        // Appending location makes results local ("منافسين في {location}") rather than generic.
        if (!string.IsNullOrWhiteSpace(brand.BrandInfo?.Location))
        {
            parts.Add($"in {brand.BrandInfo.Location}");
        }

        return string.Join(" ", parts);
    }

    private static string? TryExtractBriefField(string? briefJson, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(briefJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(briefJson);
            return doc.RootElement.TryGetProperty(fieldName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "…";
}
