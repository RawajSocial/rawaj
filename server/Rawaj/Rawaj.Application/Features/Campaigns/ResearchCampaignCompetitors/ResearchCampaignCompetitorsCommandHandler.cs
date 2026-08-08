using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.AiPipeline.Common;
using Rawaj.Application.Features.AiPipeline.Services;

namespace Rawaj.Application.Features.Campaigns.ResearchCampaignCompetitors;

/// <summary>
/// C19 cutover: a thin shim onto the pipeline orchestrator, request/response contract unchanged.
/// <c>CompetitorResearch</c> is one of several stages an <see cref="IPipelineOrchestrator.AdvanceAsync"/>
/// call can reach in one pass, so this and <c>GenerateBusinessDiagnosisCommandHandler</c>/
/// <c>GenerateMarketingPlanCommandHandler</c> — the frontend's <c>runPipeline</c> chain calls all
/// three back to back with no user decision in between — will often see the whole strategy already
/// computed by the time this returns; each just reads back the one column it's named after.
/// </summary>
public class ResearchCampaignCompetitorsCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IPipelineOrchestrator orchestrator)
    : IRequestHandler<ResearchCampaignCompetitorsCommand, Result<ResearchCampaignCompetitorsResponse>>
{
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

        var run = await LegacyPipelineGateway.EnsureRunAdvancedAsync(
            dbContext, orchestrator, campaign, brand, userId, role, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (campaign.CompetitorResearchJson is null)
        {
            var blockingError = await LegacyPipelineGateway.BlockingErrorAsync(dbContext, run.Id, cancellationToken);
            return Result<ResearchCampaignCompetitorsResponse>.Failure(
                blockingError ?? "Competitor research could not be completed. Please try again.");
        }

        var (found, note) = ParseAvailability(campaign.CompetitorResearchJson);

        return Result<ResearchCampaignCompetitorsResponse>.Success(
            new ResearchCampaignCompetitorsResponse(campaign.Id, campaign.CompetitorResearchJson, found, note, DateTime.UtcNow));
    }

    private static (bool found, string? note) ParseAvailability(string competitorResearchJson)
    {
        using var doc = JsonDocument.Parse(competitorResearchJson);
        var unavailable = doc.RootElement.TryGetProperty("unavailable", out var u) && u.GetBoolean();
        var note = doc.RootElement.TryGetProperty("note", out var n) && n.ValueKind == JsonValueKind.String
            ? n.GetString()
            : null;

        return (!unavailable, note);
    }
}
