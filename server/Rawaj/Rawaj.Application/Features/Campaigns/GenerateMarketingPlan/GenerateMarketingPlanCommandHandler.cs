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
    IAiTextGenerationService textGenerationService,
    ICoinCostProvider coinCostProvider)
    : IRequestHandler<GenerateMarketingPlanCommand, Result<GenerateMarketingPlanResponse>>
{
    public async Task<Result<GenerateMarketingPlanResponse>> Handle(
        GenerateMarketingPlanCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;
        var role = currentTenantContext.Role!.Value;

        var campaign = await dbContext.MarketingCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (campaign is null)
        {
            return Result<GenerateMarketingPlanResponse>.Failure("Campaign not found.");
        }

        var brand = await dbContext.TenantBrandProfiles
            .FirstAsync(b => b.Id == campaign.BrandProfileId, cancellationToken);

        // AiCreditsPolicy's monthly quota is retired as an enforcement gate — coins are the real,
        // per-action meter now (GetAiCreditsUsage stays as a read-only stat on the billing page).
        var tenant = await dbContext.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);

        // New-tenant free trial (pricing sheet section 3.1): one free complete marketing strategy
        // per tenant, regardless of whether it's a business owner or an agency.
        var usesFreeTrial = !tenant.FreeMarketingPlanUsed;
        var coinCost = usesFreeTrial
            ? 0
            : await CoinPricingPolicy.GetDiscountedCostAsync(dbContext, tenantId, coinCostProvider.MarketingPlanGeneration, cancellationToken);
        var coinBalance = await CoinPolicy.GetBalanceAsync(dbContext, tenantId, userId, role, cancellationToken);
        if (coinBalance < coinCost)
        {
            return Result<GenerateMarketingPlanResponse>.Failure(
                CoinPolicy.InsufficientCoinsMessage(coinCost, coinBalance, "generate a marketing plan"));
        }

        // Grounded in the brief + business diagnosis (both already confirmed by the user via the
        // approval flow) with competitor research folded in as advisory-only input.
        var prompt = ContentPromptBuilder.BuildCampaignStrategyPrompt(
            brand, campaign, campaign.BriefJson, campaign.DiagnosisJson, campaign.CompetitorResearchJson);
        var startedAt = DateTime.UtcNow;
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
            StartedAt = startedAt,
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

        // The model is asked for bare JSON but doesn't always comply (markdown fences, a line of
        // commentary). Store only what actually parses — a column named AiPlanJson that holds a
        // fenced blob passes the "a plan exists" check everywhere while rendering as nothing in
        // the review UI, which is indistinguishable from a broken page for the user who paid for it.
        var planJson = AiJsonResponseParser.ExtractJsonPayload(generation.Text);
        if (planJson is null)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<GenerateMarketingPlanResponse>.Failure(
                "The generated marketing plan could not be read. Please try again.");
        }

        campaign.AiPlanJson = planJson;
        campaign.AiGeneratedAt = now;
        campaign.UpdatedAt = now;

        if (usesFreeTrial)
        {
            tenant.FreeMarketingPlanUsed = true;
        }
        else
        {
            await CoinPolicy.TrySpendAsync(dbContext, tenantId, userId, role, coinCost, cancellationToken, reason: "marketing_plan_generation");
        }

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
