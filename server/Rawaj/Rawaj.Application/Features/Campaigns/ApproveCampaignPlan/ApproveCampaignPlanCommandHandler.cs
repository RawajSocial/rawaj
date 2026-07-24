using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.ApproveCampaignPlan;

/// <summary>
/// Stamps PlanApprovedAt once the user signs off on the generated strategy — this is the gate
/// GenerateCampaignContentCommandHandler checks before it will produce any posts, so nothing gets
/// generated against a plan nobody has actually reviewed and approved.
/// </summary>
public class ApproveCampaignPlanCommandHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<ApproveCampaignPlanCommand, Result<ApproveCampaignPlanResponse>>
{
    public async Task<Result<ApproveCampaignPlanResponse>> Handle(
        ApproveCampaignPlanCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var campaign = await dbContext.MarketingCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (campaign is null)
        {
            return Result<ApproveCampaignPlanResponse>.Failure("Campaign not found.");
        }

        if (string.IsNullOrWhiteSpace(campaign.AiPlanJson))
        {
            return Result<ApproveCampaignPlanResponse>.Failure("Generate a strategy before approving it.");
        }

        var now = DateTime.UtcNow;
        campaign.PlanApprovedAt = now;
        if (campaign.Status == CampaignStatus.Draft)
        {
            campaign.Status = CampaignStatus.Active;
        }
        campaign.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<ApproveCampaignPlanResponse>.Success(
            new ApproveCampaignPlanResponse(campaign.Id, campaign.Status, campaign.PlanApprovedAt.Value));
    }
}
