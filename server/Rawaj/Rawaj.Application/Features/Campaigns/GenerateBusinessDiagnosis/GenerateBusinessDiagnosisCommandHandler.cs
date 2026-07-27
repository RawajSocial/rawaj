using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Features.Content.Common;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.GenerateBusinessDiagnosis;

/// <summary>
/// "What we understood about your business" — the AI Business Diagnosis pricing-sheet feature.
/// Grounds the diagnosis in the onboarding brief plus whatever (possibly empty/unavailable)
/// competitor research already ran, and stores the result on campaign.DiagnosisJson for the
/// approval UI to render before the campaign strategy itself is generated.
/// </summary>
public class GenerateBusinessDiagnosisCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IAiTextGenerationService textGenerationService,
    ICoinCostProvider coinCostProvider)
    : IRequestHandler<GenerateBusinessDiagnosisCommand, Result<GenerateBusinessDiagnosisResponse>>
{
    public async Task<Result<GenerateBusinessDiagnosisResponse>> Handle(
        GenerateBusinessDiagnosisCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;
        var role = currentTenantContext.Role!.Value;

        var campaign = await dbContext.MarketingCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (campaign is null)
        {
            return Result<GenerateBusinessDiagnosisResponse>.Failure("Campaign not found.");
        }

        var brand = await dbContext.TenantBrandProfiles.FirstAsync(b => b.Id == campaign.BrandProfileId, cancellationToken);

        var coinCost = await CoinPricingPolicy.GetDiscountedCostAsync(dbContext, tenantId, coinCostProvider.BusinessDiagnosis, cancellationToken);
        var coinBalance = await CoinPolicy.GetBalanceAsync(dbContext, tenantId, userId, role, cancellationToken);
        if (coinBalance < coinCost)
        {
            return Result<GenerateBusinessDiagnosisResponse>.Failure(
                CoinPolicy.InsufficientCoinsMessage(coinCost, coinBalance, "get a business diagnosis"));
        }

        var prompt = ContentPromptBuilder.BuildBusinessDiagnosisPrompt(brand, campaign.BriefJson, campaign.CompetitorResearchJson);
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
            return Result<GenerateBusinessDiagnosisResponse>.Failure(
                generation.ErrorMessage ?? "Business diagnosis failed. Please try again.");
        }

        // Only store what parses — see AiJsonResponseParser. Charging for a diagnosis the review
        // UI can't render is the same failure as not producing one.
        var diagnosisJson = AiJsonResponseParser.ExtractJsonPayload(generation.Text);
        if (diagnosisJson is null)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<GenerateBusinessDiagnosisResponse>.Failure(
                "The generated business diagnosis could not be read. Please try again.");
        }

        campaign.DiagnosisJson = diagnosisJson;
        campaign.UpdatedAt = now;

        await CoinPolicy.TrySpendAsync(dbContext, tenantId, userId, role, coinCost, cancellationToken, reason: "business_diagnosis");

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<GenerateBusinessDiagnosisResponse>.Success(
            new GenerateBusinessDiagnosisResponse(campaign.Id, campaign.DiagnosisJson, now));
    }
}
