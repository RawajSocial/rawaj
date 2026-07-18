using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Entities.Campaigns;

namespace Rawaj.Persistence.Campaigns;

public class CampaignOnboardingService(AppDbContext dbContext) : ICampaignOnboardingService
{
    public Task<bool> UserHasBrandAccessAsync(Guid userId, Guid brandProfileId, CancellationToken cancellationToken) =>
        dbContext.TenantMemberBrandAccesses
            .Where(a => a.BrandProfileId == brandProfileId)
            .Join(dbContext.TenantMembers, a => a.TenantMemberId, m => m.Id, (a, m) => m)
            .AnyAsync(m => m.UserId == userId, cancellationToken);

    public async Task<MarketingCampaign> CreateDraftAsync(MarketingCampaign campaign, CancellationToken cancellationToken)
    {
        dbContext.MarketingCampaigns.Add(campaign);
        await dbContext.SaveChangesAsync(cancellationToken);
        return campaign;
    }

    public Task<MarketingCampaign?> GetByIdAsync(Guid campaignId, CancellationToken cancellationToken) =>
        dbContext.MarketingCampaigns.FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
