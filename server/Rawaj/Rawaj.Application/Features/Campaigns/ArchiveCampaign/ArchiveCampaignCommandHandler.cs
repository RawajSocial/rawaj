using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.ArchiveCampaign;

public class ArchiveCampaignCommandHandler(
    IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext, ICurrentUserService currentUserService)
    : IRequestHandler<ArchiveCampaignCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ArchiveCampaignCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var userId = currentUserService.UserId!.Value;

        var campaign = await dbContext.MarketingCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (campaign is null)
        {
            return Result<bool>.Failure("Campaign not found.");
        }

        if (campaign.Status != CampaignStatus.Archived)
        {
            campaign.Status = CampaignStatus.Archived;
            campaign.UpdatedAt = DateTime.UtcNow;

            NotificationPublisher.Notify(
                dbContext, userId, campaign.BrandProfileId,
                NotificationType.Info, NotificationCategory.System,
                "Campaign archived",
                $"\"{campaign.Name}\" was archived.",
                campaign.Id, "marketing_campaign");

            AuditLogger.Log(
                dbContext, tenantId, userId, "campaign.archived",
                message: $"Archived campaign \"{campaign.Name}\".",
                entityType: "marketing_campaign", entityId: campaign.Id, brandProfileId: campaign.BrandProfileId);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result<bool>.Success(true);
    }
}
