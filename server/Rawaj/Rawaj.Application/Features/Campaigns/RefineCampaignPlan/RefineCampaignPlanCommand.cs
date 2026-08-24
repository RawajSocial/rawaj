using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.RefineCampaignPlan;

public record RefineCampaignPlanCommand(Guid CampaignId, string Feedback)
    : IRequest<Result<RefineCampaignPlanResponse>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.MarketingCampaigns.Where(c => c.Id == CampaignId).Select(c => (Guid?)c.BrandProfileId).FirstOrDefaultAsync(cancellationToken);
}
