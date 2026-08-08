using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.AiPipeline.GetArtifact;

/// <param name="CampaignId">Every artifact kind resolves through a campaign, including the
/// brand-scoped <c>BrandAnalysis</c> — <see cref="IPipelineArtifactStore"/> only needs the brand id
/// for that one, which it reaches through the campaign here.</param>
public record GetArtifactQuery(Guid CampaignId, AiArtifactKind Kind)
    : IRequest<Result<GetArtifactResponse>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.MarketingCampaigns.Where(c => c.Id == CampaignId).Select(c => (Guid?)c.BrandProfileId).FirstOrDefaultAsync(cancellationToken);
}
