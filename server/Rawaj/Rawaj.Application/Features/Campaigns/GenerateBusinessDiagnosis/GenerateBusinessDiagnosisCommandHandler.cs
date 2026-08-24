using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.AiPipeline.Common;
using Rawaj.Application.Features.AiPipeline.Services;

namespace Rawaj.Application.Features.Campaigns.GenerateBusinessDiagnosis;

/// <summary>
/// C19 cutover: a thin shim onto the pipeline orchestrator, request/response contract unchanged.
/// <c>DiagnosisJson</c> comes from <c>CampaignAnalysis</c> completing (composed with the brand's
/// <c>BrandAnalysis</c> — see <c>LegacyCampaignProjection.ProjectDiagnosis</c>), which
/// <see cref="ResearchCampaignCompetitors.ResearchCampaignCompetitorsCommandHandler"/> may already
/// have triggered if it ran first in the frontend's call chain.
/// </summary>
public class GenerateBusinessDiagnosisCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IPipelineOrchestrator orchestrator)
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

        var run = await LegacyPipelineGateway.EnsureRunAdvancedAsync(
            dbContext, orchestrator, campaign, brand, userId, role, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (campaign.DiagnosisJson is null)
        {
            var blockingError = await LegacyPipelineGateway.BlockingErrorAsync(dbContext, run.Id, cancellationToken);
            return Result<GenerateBusinessDiagnosisResponse>.Failure(
                blockingError ?? "Business diagnosis failed. Please try again.");
        }

        return Result<GenerateBusinessDiagnosisResponse>.Success(
            new GenerateBusinessDiagnosisResponse(campaign.Id, campaign.DiagnosisJson, DateTime.UtcNow));
    }
}
