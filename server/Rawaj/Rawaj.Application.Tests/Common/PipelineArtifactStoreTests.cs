using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Services;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Persistence;
using Xunit;

namespace Rawaj.Application.Tests.Common;

/// <summary>
/// Artifact versioning. The behaviour that matters here is that a new version never destroys the
/// previous one — today RefineCampaignPlan overwrites AiPlanJson in place, so a user who refines
/// twice cannot get back the version they preferred, and an approval recorded as a bare timestamp
/// cannot say which strategy it approved.
/// </summary>
public class PipelineArtifactStoreTests
{
    [Fact]
    public async Task NewVersion_SupersedesTheOldOne_WithoutDeletingIt()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var (tenantId, brandId, campaignId) = await SeedAsync(dbContext);
        var store = new PipelineArtifactStore(dbContext);

        await store.AddVersionAsync(
            Stage(), tenantId, brandId, campaignId, AiArtifactKind.Strategy,
            """{"executiveSummary":"first"}""", null, CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        await store.AddVersionAsync(
            Stage(), tenantId, brandId, campaignId, AiArtifactKind.Strategy,
            """{"executiveSummary":"refined"}""", null, CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var current = await store.GetCurrentAsync(brandId, campaignId, AiArtifactKind.Strategy, CancellationToken.None);
        var original = await store.GetVersionAsync(brandId, campaignId, AiArtifactKind.Strategy, 1, CancellationToken.None);

        Assert.NotNull(current);
        Assert.Equal(2, current.Version);
        Assert.Contains("refined", current.ContentJson);

        Assert.NotNull(original);
        Assert.False(original.IsCurrent);
        Assert.Contains("first", original.ContentJson);

        // Exactly one current row, always.
        var currentCount = await dbContext.AiArtifacts
            .CountAsync(a => a.CampaignId == campaignId && a.Kind == AiArtifactKind.Strategy && a.IsCurrent);
        Assert.Equal(1, currentCount);
    }

    [Fact]
    public async Task BrandAnalysis_IsStoredAgainstTheBrand_NotTheCampaign()
    {
        // This is what lets one brand analysis serve every campaign of that brand.
        await using var dbContext = TestDbContextFactory.Create();
        var (tenantId, brandId, campaignId) = await SeedAsync(dbContext);
        var store = new PipelineArtifactStore(dbContext);

        await store.AddVersionAsync(
            Stage(), tenantId, brandId, campaignId, AiArtifactKind.BrandAnalysis,
            """{"identityCore":"specialty coffee"}""", "hash-a", CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var stored = await store.GetCurrentAsync(brandId, campaignId, AiArtifactKind.BrandAnalysis, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Null(stored.CampaignId);

        // A different campaign of the same brand still finds it.
        var fromAnotherCampaign = await store.GetCurrentAsync(
            brandId, Guid.NewGuid(), AiArtifactKind.BrandAnalysis, CancellationToken.None);
        Assert.NotNull(fromAnotherCampaign);
        Assert.Equal(stored.Id, fromAnotherCampaign.Id);
    }

    [Fact]
    public async Task CacheLookup_HitsOnMatchingInputHash_AndMissesWhenTheBrandChanged()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var (tenantId, brandId, campaignId) = await SeedAsync(dbContext);
        var store = new PipelineArtifactStore(dbContext);

        await store.AddVersionAsync(
            Stage(), tenantId, brandId, campaignId, AiArtifactKind.BrandAnalysis,
            """{"identityCore":"x"}""", "hash-a", CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var hit = await store.FindByInputHashAsync(brandId, AiArtifactKind.BrandAnalysis, "hash-a", CancellationToken.None);
        var miss = await store.FindByInputHashAsync(brandId, AiArtifactKind.BrandAnalysis, "hash-b", CancellationToken.None);

        Assert.NotNull(hit);
        Assert.Null(miss);
    }

    [Fact]
    public async Task MissingKinds_AreAbsentFromInputs_RatherThanPresentAndEmpty()
    {
        // An optional stage that was skipped has nothing to contribute, and executors branch on
        // absence. A null entry would make "skipped" indistinguishable from "returned nothing".
        await using var dbContext = TestDbContextFactory.Create();
        var (tenantId, brandId, campaignId) = await SeedAsync(dbContext);
        var store = new PipelineArtifactStore(dbContext);

        await store.AddVersionAsync(
            Stage(), tenantId, brandId, campaignId, AiArtifactKind.CampaignAnalysis,
            """{"objectiveClassification":"awareness"}""", null, CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var inputs = await store.GetCurrentPayloadsAsync(
            brandId, campaignId,
            [AiArtifactKind.CampaignAnalysis, AiArtifactKind.MarketResearch, AiArtifactKind.CompetitorResearch],
            CancellationToken.None);

        Assert.True(inputs.ContainsKey(AiArtifactKind.CampaignAnalysis));
        Assert.False(inputs.ContainsKey(AiArtifactKind.MarketResearch));
        Assert.False(inputs.ContainsKey(AiArtifactKind.CompetitorResearch));
    }

    [Fact]
    public async Task GetCurrentPayloads_ResolvesBrandAndCampaignScopedKinds_Together()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var (tenantId, brandId, campaignId) = await SeedAsync(dbContext);
        var store = new PipelineArtifactStore(dbContext);

        await store.AddVersionAsync(
            Stage(), tenantId, brandId, campaignId, AiArtifactKind.BrandAnalysis,
            """{"identityCore":"brand"}""", null, CancellationToken.None);
        await store.AddVersionAsync(
            Stage(), tenantId, brandId, campaignId, AiArtifactKind.CampaignAnalysis,
            """{"objectiveClassification":"campaign"}""", null, CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var inputs = await store.GetCurrentPayloadsAsync(
            brandId, campaignId,
            [AiArtifactKind.BrandAnalysis, AiArtifactKind.CampaignAnalysis],
            CancellationToken.None);

        Assert.Equal(2, inputs.Count);
        Assert.Contains("brand", inputs[AiArtifactKind.BrandAnalysis]);
        Assert.Contains("campaign", inputs[AiArtifactKind.CampaignAnalysis]);
    }

    [Fact]
    public async Task ProducingStage_IsRecordedOnBothSides()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var (tenantId, brandId, campaignId) = await SeedAsync(dbContext);
        var store = new PipelineArtifactStore(dbContext);
        var stage = Stage();

        var artifact = await store.AddVersionAsync(
            stage, tenantId, brandId, campaignId, AiArtifactKind.Strategy, "{}", null, CancellationToken.None);

        Assert.Equal(stage.Id, artifact.SourceStageId);
        Assert.Equal(artifact.Id, stage.ArtifactId);
    }

    [Fact]
    public async Task AddVersionAsync_AcceptsNoBackingStage_ForAWriteThatIsNotAGraphNode()
    {
        // Strategy refinement is a versioned write with no stage row behind it — the graph has no
        // node for "user typed feedback into a box".
        await using var dbContext = TestDbContextFactory.Create();
        var (tenantId, brandId, campaignId) = await SeedAsync(dbContext);
        var store = new PipelineArtifactStore(dbContext);

        var artifact = await store.AddVersionAsync(
            stage: null, tenantId, brandId, campaignId, AiArtifactKind.Strategy, "{}", null, CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        Assert.Null(artifact.SourceStageId);
        Assert.True(artifact.IsCurrent);
    }

    private static AiPipelineStage Stage() => new()
    {
        Id = Guid.NewGuid(),
        RunId = Guid.NewGuid(),
        Kind = AiPipelineStageKind.StrategyAssemble,
        Status = AiPipelineStageStatus.Running,
        CreatedAt = DateTime.UtcNow
    };

    private static async Task<(Guid TenantId, Guid BrandProfileId, Guid CampaignId)> SeedAsync(AppDbContext dbContext)
    {
        var now = DateTime.UtcNow;
        var planId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var brandProfileId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();

        dbContext.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId, Name = "Free", Cost = 0, Currency = "USD",
            MaxCampaignsMonthly = 100, IsActive = true, CreatedAt = now
        });

        dbContext.Subscriptions.Add(new Subscription
        {
            Id = subscriptionId, SubscriptionPlanId = planId, Status = SubscriptionStatus.Active,
            CurrentPeriodStart = now, CurrentPeriodEnd = now.AddMonths(1), CreatedAt = now
        });

        dbContext.Tenants.Add(new Tenant
        {
            Id = tenantId, Name = "Test Tenant", Subdomain = Guid.NewGuid().ToString("N"),
            TenantType = TenantType.Business, OwnerUserId = Guid.NewGuid(), SubscriptionId = subscriptionId,
            IsActive = true, IsActivated = true, CreatedAt = now, UpdatedAt = now
        });

        dbContext.TenantBrandProfiles.Add(new TenantBrandProfile
        {
            Id = brandProfileId, TenantId = tenantId, Name = "Test Brand",
            Status = BrandProfileStatus.Active, CreatedAt = now, UpdatedAt = now
        });

        dbContext.MarketingCampaigns.Add(new MarketingCampaign
        {
            Id = campaignId, BrandProfileId = brandProfileId, CreatedBy = Guid.NewGuid(),
            Name = "Test Campaign", Status = CampaignStatus.Draft, TargetPlatforms = [],
            CreatedAt = now, UpdatedAt = now
        });

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return (tenantId, brandProfileId, campaignId);
    }
}
