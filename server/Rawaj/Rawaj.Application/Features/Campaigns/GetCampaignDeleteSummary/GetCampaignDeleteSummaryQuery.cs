using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.GetCampaignDeleteSummary;

/// <summary>Powers the delete-confirmation modal — what's about to be deleted, before the user
/// commits to it. Read-only, so it's Viewer-accessible like every other campaign read; the delete
/// action itself is the one gated at Admin.</summary>
public record GetCampaignDeleteSummaryQuery(Guid CampaignId)
    : IRequest<Result<CampaignDeleteSummaryResponse>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.MarketingCampaigns.Where(c => c.Id == CampaignId).Select(c => (Guid?)c.BrandProfileId).FirstOrDefaultAsync(cancellationToken);
}
