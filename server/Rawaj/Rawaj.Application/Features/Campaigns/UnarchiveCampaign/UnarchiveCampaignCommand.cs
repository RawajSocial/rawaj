using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.UnarchiveCampaign;

/// <summary>
/// Restores an archived campaign. Archiving was previously a one-way door — nothing in the API
/// could move a campaign back out of <see cref="CampaignStatus.Archived"/>, so a campaign archived
/// by mistake (or by the onboarding wizard abandoning a draft the user actually wanted) was lost
/// from every list for good.
/// </summary>
public record UnarchiveCampaignCommand(Guid CampaignId)
    : IRequest<Result<UnarchiveCampaignResponse>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.MarketingCampaigns.Where(c => c.Id == CampaignId).Select(c => (Guid?)c.BrandProfileId).FirstOrDefaultAsync(cancellationToken);
}
