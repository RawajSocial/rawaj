using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.GetCampaignDeleteSummary;

public class GetCampaignDeleteSummaryQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetCampaignDeleteSummaryQuery, Result<CampaignDeleteSummaryResponse>>
{
    public async Task<Result<CampaignDeleteSummaryResponse>> Handle(
        GetCampaignDeleteSummaryQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var campaign = await dbContext.MarketingCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (campaign is null)
        {
            return Result<CampaignDeleteSummaryResponse>.Failure("Campaign not found.");
        }

        var contentItemCount = await dbContext.ContentItems
            .CountAsync(c => c.CampaignId == campaign.Id, cancellationToken);

        // Matches DeleteCampaignCommandHandler's own lookup: an asset counts if it's tagged with
        // this campaign directly, or belongs to one of the campaign's content items — every known
        // creation site sets CampaignId whenever a campaign exists, but this is the same
        // belt-and-braces query the delete itself uses, so the preview can never undercount what
        // the delete will actually remove.
        var imageCount = await dbContext.VisualAssets
            .CountAsync(v => v.CampaignId == campaign.Id || (v.ContentItemId != null && v.ContentItem!.CampaignId == campaign.Id), cancellationToken);

        var pendingScheduledCount = await dbContext.ScheduledPosts
            .CountAsync(s => s.CampaignId == campaign.Id && s.Status == ScheduledPostStatus.Pending, cancellationToken);

        var publishedScheduledCount = await dbContext.ScheduledPosts
            .CountAsync(s => s.CampaignId == campaign.Id && s.Status == ScheduledPostStatus.Published, cancellationToken);

        return Result<CampaignDeleteSummaryResponse>.Success(new CampaignDeleteSummaryResponse(
            campaign.Id, campaign.Name, contentItemCount, imageCount, pendingScheduledCount, publishedScheduledCount));
    }
}
