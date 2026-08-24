using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.AiPipeline.GetArtifact;

public class GetArtifactQueryHandler(
    IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext, IPipelineArtifactStore artifacts)
    : IRequestHandler<GetArtifactQuery, Result<GetArtifactResponse>>
{
    public async Task<Result<GetArtifactResponse>> Handle(GetArtifactQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var campaign = await dbContext.MarketingCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (campaign is null)
        {
            return Result<GetArtifactResponse>.Failure("Campaign not found.");
        }

        var artifact = await artifacts.GetCurrentAsync(campaign.BrandProfileId, campaign.Id, request.Kind, cancellationToken);
        if (artifact is null)
        {
            return Result<GetArtifactResponse>.Failure("This artifact has not been generated yet.");
        }

        return Result<GetArtifactResponse>.Success(
            new GetArtifactResponse(artifact.Kind, artifact.Version, artifact.ContentJson, artifact.CreatedAt));
    }
}
