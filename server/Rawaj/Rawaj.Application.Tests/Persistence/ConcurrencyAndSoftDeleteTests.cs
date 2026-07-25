using Microsoft.EntityFrameworkCore;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Xunit;

namespace Rawaj.Application.Tests.Persistence;

/// <summary>
/// Phase 9 verification: RowVersion optimistic concurrency (via AppDbContext's manual bump for
/// entities implementing IConcurrencyAware — see AppDbContext.SaveChangesAsync) and the soft-delete
/// global query filters, exercised directly against the DbContext rather than through a specific
/// command handler since the behavior is shared infrastructure, not feature-specific logic.
/// </summary>
public class ConcurrencyAndSoftDeleteTests
{
    [Fact]
    public async Task SecondConcurrentSave_OnSameCampaign_ThrowsDbUpdateConcurrencyException()
    {
        var dbName = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var (tenantId, brandProfileId) = await SeedTenantAndBrandAsync(dbName, now);

        var campaignId = Guid.NewGuid();
        await using (var seedContext = TestDbContextFactory.Create(dbName))
        {
            seedContext.MarketingCampaigns.Add(new MarketingCampaign
            {
                Id = campaignId,
                BrandProfileId = brandProfileId,
                CreatedBy = Guid.NewGuid(),
                Name = "Original Name",
                Status = CampaignStatus.Draft,
                TargetPlatforms = [],
                CreatedAt = now,
                UpdatedAt = now
            });
            await seedContext.SaveChangesAsync(CancellationToken.None);
        }

        // Two independent contexts load the same row — simulating two overlapping edit requests.
        await using var contextA = TestDbContextFactory.Create(dbName);
        await using var contextB = TestDbContextFactory.Create(dbName);

        var campaignA = await contextA.MarketingCampaigns.FirstAsync(c => c.Id == campaignId);
        var campaignB = await contextB.MarketingCampaigns.FirstAsync(c => c.Id == campaignId);

        campaignA.Name = "Edited by A";
        await contextA.SaveChangesAsync(CancellationToken.None);

        campaignB.Name = "Edited by B";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => contextB.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SequentialSaves_OnSameCampaign_DoNotConflict()
    {
        var dbName = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var (tenantId, brandProfileId) = await SeedTenantAndBrandAsync(dbName, now);

        var campaignId = Guid.NewGuid();
        await using (var seedContext = TestDbContextFactory.Create(dbName))
        {
            seedContext.MarketingCampaigns.Add(new MarketingCampaign
            {
                Id = campaignId,
                BrandProfileId = brandProfileId,
                CreatedBy = Guid.NewGuid(),
                Name = "Original Name",
                Status = CampaignStatus.Draft,
                TargetPlatforms = [],
                CreatedAt = now,
                UpdatedAt = now
            });
            await seedContext.SaveChangesAsync(CancellationToken.None);
        }

        // Load-edit-save, then load-edit-save again — no overlap, so no conflict expected.
        await using (var contextA = TestDbContextFactory.Create(dbName))
        {
            var campaign = await contextA.MarketingCampaigns.FirstAsync(c => c.Id == campaignId);
            campaign.Name = "First Edit";
            await contextA.SaveChangesAsync(CancellationToken.None);
        }

        await using (var contextB = TestDbContextFactory.Create(dbName))
        {
            var campaign = await contextB.MarketingCampaigns.FirstAsync(c => c.Id == campaignId);
            campaign.Name = "Second Edit";
            await contextB.SaveChangesAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task SoftDeletedContentItem_IsExcludedFromDefaultQueries_ButVisibleWithIgnoreQueryFilters()
    {
        var dbName = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var (tenantId, brandProfileId) = await SeedTenantAndBrandAsync(dbName, now);

        var contentItemId = Guid.NewGuid();
        await using (var seedContext = TestDbContextFactory.Create(dbName))
        {
            seedContext.ContentItems.Add(new ContentItem
            {
                Id = contentItemId,
                TenantId = tenantId,
                BrandProfileId = brandProfileId,
                GenerationMode = GenerationMode.Brand,
                CreatedBy = Guid.NewGuid(),
                ContentType = ContentType.Caption,
                Platform = SocialPlatform.Instagram,
                Language = Language.Ar,
                Content = "Test content",
                Status = ContentStatus.Draft,
                CreatedAt = now,
                UpdatedAt = now
            });
            await seedContext.SaveChangesAsync(CancellationToken.None);
        }

        await using (var deleteContext = TestDbContextFactory.Create(dbName))
        {
            var item = await deleteContext.ContentItems.FirstAsync(c => c.Id == contentItemId);
            item.IsDeleted = true;
            item.DeletedAt = now;
            item.DeletedBy = Guid.NewGuid();
            await deleteContext.SaveChangesAsync(CancellationToken.None);
        }

        await using var readContext = TestDbContextFactory.Create(dbName);

        var visibleByDefault = await readContext.ContentItems.AnyAsync(c => c.Id == contentItemId);
        Assert.False(visibleByDefault);

        var visibleIgnoringFilter = await readContext.ContentItems
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Id == contentItemId);
        Assert.True(visibleIgnoringFilter);
    }

    private static async Task<(Guid TenantId, Guid BrandProfileId)> SeedTenantAndBrandAsync(string dbName, DateTime now)
    {
        await using var dbContext = TestDbContextFactory.Create(dbName);

        var planId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var brandProfileId = Guid.NewGuid();

        dbContext.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "Free",
            Cost = 0,
            Currency = "USD",
            MaxCampaignsMonthly = 100,
            IsActive = true,
            CreatedAt = now
        });

        dbContext.Subscriptions.Add(new Subscription
        {
            Id = subscriptionId,
            SubscriptionPlanId = planId,
            Status = SubscriptionStatus.Active,
            CurrentPeriodStart = now,
            CurrentPeriodEnd = now.AddMonths(1),
            CreatedAt = now
        });

        dbContext.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Test Tenant",
            Subdomain = Guid.NewGuid().ToString("N"),
            TenantType = TenantType.Agency,
            OwnerUserId = Guid.NewGuid(),
            SubscriptionId = subscriptionId,
            IsActive = true,
            IsActivated = true,
            CreatedAt = now,
            UpdatedAt = now
        });

        dbContext.TenantBrandProfiles.Add(new TenantBrandProfile
        {
            Id = brandProfileId,
            TenantId = tenantId,
            Name = "Test Brand",
            Status = BrandProfileStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        });

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return (tenantId, brandProfileId);
    }
}
