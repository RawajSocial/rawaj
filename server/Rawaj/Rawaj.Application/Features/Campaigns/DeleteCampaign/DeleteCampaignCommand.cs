using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.DeleteCampaign;

/// <summary>Permanently removes a campaign from every view of the product — soft-delete, not a
/// SQL row delete (see the handler's doc comment for why). Gated stricter than every other campaign
/// action (Admin, not Editor): it cascades to content, images and scheduled posts, and coins already
/// spent are not refunded, so this deserves a higher bar than "anyone who can edit."</summary>
public record DeleteCampaignCommand(Guid CampaignId)
    : IRequest<Result<DeleteCampaignResponse>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Admin;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.MarketingCampaigns.Where(c => c.Id == CampaignId).Select(c => (Guid?)c.BrandProfileId).FirstOrDefaultAsync(cancellationToken);
}
