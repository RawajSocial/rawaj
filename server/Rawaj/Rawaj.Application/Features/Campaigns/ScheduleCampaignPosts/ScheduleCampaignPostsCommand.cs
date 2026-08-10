using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.ScheduleCampaignPosts;

/// <summary>
/// Schedules every approved, not-yet-scheduled content item in the campaign, routing each item to
/// the brand's connected active account matching that item's own platform - a campaign spanning
/// Instagram and Facebook posts to both, rather than forcing a single caller-picked account.
/// </summary>
public record ScheduleCampaignPostsCommand(Guid CampaignId, bool PublishPastDueNow = false)
    : IRequest<Result<ScheduleCampaignPostsResponse>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.MarketingCampaigns.Where(c => c.Id == CampaignId).Select(c => (Guid?)c.BrandProfileId).FirstOrDefaultAsync(cancellationToken);
}
