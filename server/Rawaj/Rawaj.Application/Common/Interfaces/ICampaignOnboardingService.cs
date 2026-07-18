using Rawaj.Domain.Entities.Campaigns;

namespace Rawaj.Application.Common.Interfaces;

public interface ICampaignOnboardingService
{
    Task<bool> UserHasBrandAccessAsync(Guid userId, Guid brandProfileId, CancellationToken cancellationToken);

    Task<MarketingCampaign> CreateDraftAsync(MarketingCampaign campaign, CancellationToken cancellationToken);

    Task<MarketingCampaign?> GetByIdAsync(Guid campaignId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
