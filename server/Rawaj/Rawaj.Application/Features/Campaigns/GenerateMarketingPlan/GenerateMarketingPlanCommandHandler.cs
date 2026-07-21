using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.Content.Common;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.GenerateMarketingPlan;

/// <summary>
/// Synthesizes brand info, campaign details, and any competitor intelligence already gathered
/// via AnalyzeCompetitor (rag_documents rows keyed to the same brand) into a strategy, stored as
/// raw JSON text on marketing_campaigns.ai_plan_json. The model is asked to return JSON directly
/// rather than parsed/validated server-side, since the shape is advisory content for the UI to
/// render, not something the backend needs to act on structurally.
/// </summary>
public class GenerateMarketingPlanCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IAiTextGenerationService textGenerationService)
    : IRequestHandler<GenerateMarketingPlanCommand, Result<GenerateMarketingPlanResponse>>
{
    private const int MaxCompetitorInsights = 5;

    public async Task<Result<GenerateMarketingPlanResponse>> Handle(
        GenerateMarketingPlanCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;

        var campaign = await dbContext.MarketingCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (campaign is null)
        {
            return Result<GenerateMarketingPlanResponse>.Failure("Campaign not found.");
        }

        var brand = await dbContext.TenantBrandProfiles
            .FirstAsync(b => b.Id == campaign.BrandProfileId, cancellationToken);

        var creditsUsage = await AiCreditsPolicy.GetUsageAsync(dbContext, tenantId, cancellationToken);
        if (!creditsUsage.HasCreditsRemaining)
        {
            return Result<GenerateMarketingPlanResponse>.Failure(
                $"Your subscription plan allows {creditsUsage.MaxCreditsMonthly} AI credits per month. Upgrade for more.");
        }

        var competitorInsights = await dbContext.RagDocuments
            .Where(d => d.BrandProfileId == brand.Id && d.CompetitorsData != null)
            .OrderByDescending(d => d.CreatedAt)
            .Take(MaxCompetitorInsights)
            .Select(d => d.CompetitorsData!)
            .ToListAsync(cancellationToken);

        var prompt = ContentPromptBuilder.BuildMarketingPlanPrompt(brand, campaign, competitorInsights);
        var generation = await textGenerationService.GenerateTextAsync(prompt, cancellationToken);

        var now = DateTime.UtcNow;

        var job = new AiJob
        {
            Id = Guid.NewGuid(),
            BrandProfileId = brand.Id,
            TriggeredBy = userId,
            JobType = AiJobType.PlanGeneration,
            Status = generation.Succeeded ? AiJobStatus.Completed : AiJobStatus.Failed,
            InputParams = JsonSerializer.Serialize(new { prompt }),
            OutputRefId = generation.Succeeded ? campaign.Id : null,
            OutputRefType = "marketing_campaign",
            Tokens = generation.TokensUsed,
            ErrorMessage = generation.ErrorMessage,
            StartedAt = now,
            CompletedAt = now,
            CreatedAt = now
        };
        dbContext.AiJobs.Add(job);

        if (!generation.Succeeded)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<GenerateMarketingPlanResponse>.Failure(
                generation.ErrorMessage ?? "Marketing plan generation failed. Please try again.");
        }

        campaign.AiPlanJson = generation.Text!;
        campaign.AiGeneratedAt = now;
        campaign.UpdatedAt = now;

        NotificationPublisher.Notify(
            dbContext,
            userId,
            brand.Id,
            NotificationType.Success,
            NotificationCategory.AiJob,
            "Marketing plan ready",
            $"An AI-generated strategy for \"{campaign.Name}\" is ready to review.",
            campaign.Id,
            "marketing_campaign");

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<GenerateMarketingPlanResponse>.Success(
            new GenerateMarketingPlanResponse(campaign.Id, campaign.AiPlanJson!, campaign.AiGeneratedAt.Value));
    }
}
