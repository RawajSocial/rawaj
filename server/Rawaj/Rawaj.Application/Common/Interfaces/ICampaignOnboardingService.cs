using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Interfaces;

public interface ICampaignOnboardingService
{
    Task<bool> UserHasBrandAccessAsync(Guid userId, Guid brandProfileId, CancellationToken cancellationToken);

    Task<MarketingCampaign> CreateDraftAsync(MarketingCampaign campaign, CancellationToken cancellationToken);

    Task<MarketingCampaign?> GetByIdAsync(Guid campaignId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<(List<MarketingCampaign> Items, int TotalCount)> GetPagedAsync(Guid userId, string? search, CampaignStatus? status, string? platform, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken cancellationToken);
}
