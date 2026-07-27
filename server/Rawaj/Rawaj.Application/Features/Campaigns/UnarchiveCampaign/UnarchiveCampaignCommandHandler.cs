using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.UnarchiveCampaign;

public class UnarchiveCampaignCommandHandler(
    IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext, ICurrentUserService currentUserService)
    : IRequestHandler<UnarchiveCampaignCommand, Result<UnarchiveCampaignResponse>>
{
    public async Task<Result<UnarchiveCampaignResponse>> Handle(UnarchiveCampaignCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;

        var campaign = await dbContext.MarketingCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (campaign is null)
        {
            return Result<UnarchiveCampaignResponse>.Failure("Campaign not found.");
        }

        if (campaign.Status != CampaignStatus.Archived)
        {
            return Result<UnarchiveCampaignResponse>.Failure("Only an archived campaign can be restored.");
        }

        // An approved plan is the gate GenerateCampaignContentCommandHandler checks, so a campaign
        // that had already passed it comes back Active (ready to work on) while one that hadn't
        // returns to Draft — the same states ApproveCampaignPlanCommandHandler produces. Restoring
        // everything to Draft would have silently un-approved a plan the user already paid for.
        campaign.Status = campaign.PlanApprovedAt.HasValue ? CampaignStatus.Active : CampaignStatus.Draft;
        campaign.UpdatedAt = DateTime.UtcNow;

        NotificationPublisher.Notify(
            dbContext, userId, campaign.BrandProfileId,
            NotificationType.Info, NotificationCategory.System,
            "Campaign restored",
            $"\"{campaign.Name}\" was restored from the archive.",
            campaign.Id, "marketing_campaign");

        AuditLogger.Log(
            dbContext, tenantId, userId, "campaign.unarchived",
            message: $"Restored campaign \"{campaign.Name}\".",
            entityType: "marketing_campaign", entityId: campaign.Id, brandProfileId: campaign.BrandProfileId);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<UnarchiveCampaignResponse>.Success(new UnarchiveCampaignResponse(campaign.Id, campaign.Status));
    }
}
