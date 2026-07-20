using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Enums;

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

    public async Task<(List<MarketingCampaign> Items, int TotalCount)> GetPagedAsync(Guid userId, string? search, CampaignStatus? status, string? platform, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken cancellationToken)
    {
        var memberIds = await dbContext.TenantMembers
            .Where(m => m.UserId == userId && m.InvitationStatus == InvitationStatus.Accepted)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        var brandProfileIds = await dbContext.TenantMemberBrandAccesses
            .Where(a => memberIds.Contains(a.TenantMemberId))
            .Select(a => a.BrandProfileId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var query = dbContext.MarketingCampaigns
            .Where(c => brandProfileIds.Contains(c.BrandProfileId));

        var trimmedSearch = search?.Trim();
        if (!string.IsNullOrEmpty(trimmedSearch))
        {
            // Translated to SQL LIKE by EF Core; SQL Server's default collation is case-insensitive.
            query = query.Where(c => c.Name != null && c.Name.Contains(trimmedSearch));
        }

        if (status is not null)
        {
            query = query.Where(c => c.Status == status);
        }

        if (from is not null)
        {
            query = query.Where(c => c.EndDate == null || c.EndDate >= from);
        }

        if (to is not null)
        {
            query = query.Where(c => c.StartDate == null || c.StartDate <= to);
        }

        query = query.OrderByDescending(c => c.CreatedAt);

        if (string.IsNullOrEmpty(platform))
        {
            var totalCount = await query.CountAsync(cancellationToken);
            var pagedItems = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (pagedItems, totalCount);
        }

        var narrowed = await query.ToListAsync(cancellationToken);
        var filtered = narrowed
            .Where(c => c.TargetPlatforms.Any(p => string.Equals(p, platform, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var filteredCount = filtered.Count;
        var items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return (items, filteredCount);
    }
}
