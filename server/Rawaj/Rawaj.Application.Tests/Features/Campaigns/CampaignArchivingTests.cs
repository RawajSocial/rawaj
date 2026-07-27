using NSubstitute;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Features.Campaigns.ArchiveCampaign;
using Rawaj.Application.Features.Campaigns.GetCampaigns;
using Rawaj.Application.Features.Campaigns.UnarchiveCampaign;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Persistence;
using Xunit;

namespace Rawaj.Application.Tests.Features.Campaigns;

/// <summary>
/// Archiving is the product's soft delete (the onboarding wizard archives every abandoned draft),
/// so these cover the two halves that were missing: archived campaigns leaking into the default
/// list, and archiving being a one-way door with no way back.
/// </summary>
public class CampaignArchivingTests
{
    private static async Task<(AppDbContext DbContext, Guid TenantId, Guid BrandProfileId)> SeedAsync()
    {
        var dbContext = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var brandProfileId = Guid.NewGuid();
        var now = DateTime.UtcNow;

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
        return (dbContext, tenantId, brandProfileId);
    }

    /// <summary>Campaigns are tenant-scoped through their brand profile, not a column of their own.</summary>
    private static async Task<Guid> AddCampaignAsync(
        AppDbContext dbContext, Guid brandProfileId, string name,
        CampaignStatus status, DateTime? planApprovedAt = null)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        dbContext.MarketingCampaigns.Add(new MarketingCampaign
        {
            Id = id,
            BrandProfileId = brandProfileId,
            Name = name,
            Status = status,
            PlanApprovedAt = planApprovedAt,
            TargetPlatforms = [],
            CreatedAt = now,
            UpdatedAt = now
        });

        await dbContext.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    private static (ICurrentTenantContext Tenant, ICurrentUserService User) BuildContext(Guid tenantId)
    {
        var currentTenantContext = Substitute.For<ICurrentTenantContext>();
        currentTenantContext.TenantId.Returns(tenantId);
        currentTenantContext.Role.Returns(TenantMemberRole.Owner);

        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns(Guid.NewGuid());

        return (currentTenantContext, currentUserService);
    }

    [Fact]
    public async Task GetCampaigns_ByDefault_ExcludesArchivedCampaigns()
    {
        var (dbContext, tenantId, brandProfileId) = await SeedAsync();
        await AddCampaignAsync(dbContext, brandProfileId, "Live", CampaignStatus.Active);
        await AddCampaignAsync(dbContext, brandProfileId, "Abandoned", CampaignStatus.Archived);

        var (tenant, user) = BuildContext(tenantId);
        var handler = new GetCampaignsQueryHandler(dbContext, tenant, user);

        var result = await handler.Handle(new GetCampaignsQuery(brandProfileId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Data!.TotalCount);
        Assert.Equal("Live", Assert.Single(result.Data.Items).Name);
    }

    [Fact]
    public async Task GetCampaigns_WithIncludeArchived_ReturnsArchivedCampaigns()
    {
        var (dbContext, tenantId, brandProfileId) = await SeedAsync();
        await AddCampaignAsync(dbContext, brandProfileId, "Live", CampaignStatus.Active);
        await AddCampaignAsync(dbContext, brandProfileId, "Abandoned", CampaignStatus.Archived);

        var (tenant, user) = BuildContext(tenantId);
        var handler = new GetCampaignsQueryHandler(dbContext, tenant, user);

        var result = await handler.Handle(
            new GetCampaignsQuery(brandProfileId, IncludeArchived: true), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Data!.TotalCount);
    }

    [Fact]
    public async Task Unarchive_WithApprovedPlan_RestoresToActive()
    {
        // An approved plan is the gate content generation checks — restoring to Draft would have
        // silently un-approved a strategy the tenant already paid for.
        var (dbContext, tenantId, brandProfileId) = await SeedAsync();
        var campaignId = await AddCampaignAsync(
            dbContext, brandProfileId, "Approved", CampaignStatus.Archived, DateTime.UtcNow.AddDays(-1));

        var (tenant, user) = BuildContext(tenantId);
        var handler = new UnarchiveCampaignCommandHandler(dbContext, tenant, user);

        var result = await handler.Handle(new UnarchiveCampaignCommand(campaignId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(CampaignStatus.Active, result.Data!.Status);
        Assert.Equal(CampaignStatus.Active, (await dbContext.MarketingCampaigns.FindAsync(campaignId))!.Status);
    }

    [Fact]
    public async Task Unarchive_WithoutApprovedPlan_RestoresToDraft()
    {
        var (dbContext, tenantId, brandProfileId) = await SeedAsync();
        var campaignId = await AddCampaignAsync(
            dbContext, brandProfileId, "Unapproved", CampaignStatus.Archived);

        var (tenant, user) = BuildContext(tenantId);
        var handler = new UnarchiveCampaignCommandHandler(dbContext, tenant, user);

        var result = await handler.Handle(new UnarchiveCampaignCommand(campaignId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(CampaignStatus.Draft, result.Data!.Status);
    }

    [Fact]
    public async Task Unarchive_WhenNotArchived_IsRejected()
    {
        var (dbContext, tenantId, brandProfileId) = await SeedAsync();
        var campaignId = await AddCampaignAsync(dbContext, brandProfileId, "Live", CampaignStatus.Active);

        var (tenant, user) = BuildContext(tenantId);
        var handler = new UnarchiveCampaignCommandHandler(dbContext, tenant, user);

        var result = await handler.Handle(new UnarchiveCampaignCommand(campaignId), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Only an archived campaign can be restored.", result.ErrorMessage);
    }

    [Fact]
    public async Task Unarchive_FromAnotherTenant_ReturnsNotFound()
    {
        var (dbContext, tenantId, brandProfileId) = await SeedAsync();
        var campaignId = await AddCampaignAsync(dbContext, brandProfileId, "Live", CampaignStatus.Archived);

        var (tenant, user) = BuildContext(Guid.NewGuid());
        var handler = new UnarchiveCampaignCommandHandler(dbContext, tenant, user);

        var result = await handler.Handle(new UnarchiveCampaignCommand(campaignId), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Campaign not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task ArchiveThenUnarchive_RoundTripsBackIntoTheDefaultList()
    {
        var (dbContext, tenantId, brandProfileId) = await SeedAsync();
        var campaignId = await AddCampaignAsync(dbContext, brandProfileId, "Round trip", CampaignStatus.Active);
        var (tenant, user) = BuildContext(tenantId);

        var archived = await new ArchiveCampaignCommandHandler(dbContext, tenant, user)
            .Handle(new ArchiveCampaignCommand(campaignId), CancellationToken.None);
        Assert.True(archived.Succeeded);

        var listAfterArchive = await new GetCampaignsQueryHandler(dbContext, tenant, user)
            .Handle(new GetCampaignsQuery(brandProfileId), CancellationToken.None);
        Assert.Empty(listAfterArchive.Data!.Items);

        var restored = await new UnarchiveCampaignCommandHandler(dbContext, tenant, user)
            .Handle(new UnarchiveCampaignCommand(campaignId), CancellationToken.None);
        Assert.True(restored.Succeeded);

        var listAfterRestore = await new GetCampaignsQueryHandler(dbContext, tenant, user)
            .Handle(new GetCampaignsQuery(brandProfileId), CancellationToken.None);
        Assert.Single(listAfterRestore.Data!.Items);
    }
}
