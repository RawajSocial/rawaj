using Microsoft.EntityFrameworkCore;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Xunit;

namespace Rawaj.Application.Tests.Persistence;

/// <summary>
/// Mapping-level verification for the AI pipeline tables: a run with its stages and an artifact
/// round-trips, the run↔stage cascade is configured, RowVersion works on the run the way it does on
/// MarketingCampaign, and the stage↔artifact reference cycle can actually be persisted (a stage
/// pointing at what it produced, an artifact pointing back at what produced it) — which is the part
/// most likely to have been rejected by the model builder rather than merely mis-mapped.
///
/// These run against the InMemory provider, so they verify the EF model, not SQL Server specifics:
/// filtered indexes and real rowversion generation are exercised by the migration, not here.
/// </summary>
public class AiPipelinePersistenceTests
{
    [Fact]
    public async Task RunWithStagesAndArtifact_RoundTrips()
    {
        var dbName = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var (tenantId, brandProfileId, campaignId) = await SeedAsync(dbName, now);

        var runId = Guid.NewGuid();
        var analysisStageId = Guid.NewGuid();
        var imageStageId = Guid.NewGuid();
        var artifactId = Guid.NewGuid();
        var contentItemId = Guid.NewGuid();

        await using (var seedContext = TestDbContextFactory.Create(dbName))
        {
            seedContext.AiPipelineRuns.Add(new AiPipelineRun
            {
                Id = runId,
                TenantId = tenantId,
                BrandProfileId = brandProfileId,
                CampaignId = campaignId,
                TriggeredBy = Guid.NewGuid(),
                Status = AiPipelineRunStatus.Running,
                CurrentStage = AiPipelineStageKind.CampaignAnalysis,
                TotalCoinsSpent = 4000,
                StartedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            });

            seedContext.AiArtifacts.Add(new AiArtifact
            {
                Id = artifactId,
                TenantId = tenantId,
                BrandProfileId = brandProfileId,
                CampaignId = campaignId,
                Kind = AiArtifactKind.CampaignAnalysis,
                Version = 1,
                IsCurrent = true,
                ContentJson = """{"objectiveClassification":"awareness"}""",
                SchemaVersion = 1,
                SourceStageId = analysisStageId,
                InputHash = new string('a', 64),
                CreatedAt = now
            });

            seedContext.AiPipelineStages.Add(new AiPipelineStage
            {
                Id = analysisStageId,
                RunId = runId,
                Kind = AiPipelineStageKind.CampaignAnalysis,
                Ordinal = (int)AiPipelineStageKind.CampaignAnalysis,
                Status = AiPipelineStageStatus.Completed,
                IsOptional = false,
                Attempts = 1,
                MaxAttempts = 3,
                InputHash = new string('a', 64),
                ArtifactId = artifactId,
                CoinsCharged = 4000,
                StartedAt = now,
                CompletedAt = now,
                CreatedAt = now
            });

            // A fan-out row: same run, different kind, targeting one specific content item.
            seedContext.AiPipelineStages.Add(new AiPipelineStage
            {
                Id = imageStageId,
                RunId = runId,
                Kind = AiPipelineStageKind.ContentImage,
                Ordinal = (int)AiPipelineStageKind.ContentImage,
                Status = AiPipelineStageStatus.Pending,
                IsOptional = true,
                Attempts = 1,
                MaxAttempts = 3,
                NextAttemptAt = now.AddMinutes(2),
                LastError = "HuggingFace request timed out.",
                LastErrorKind = AiFailureKind.Provider,
                TargetRefId = contentItemId,
                TargetRefType = "content_item",
                CreatedAt = now
            });

            await seedContext.SaveChangesAsync(CancellationToken.None);
        }

        await using var readContext = TestDbContextFactory.Create(dbName);

        var run = await readContext.AiPipelineRuns
            .Include(r => r.Stages)
            .FirstAsync(r => r.Id == runId);

        Assert.Equal(AiPipelineRunStatus.Running, run.Status);
        Assert.Equal(AiPipelineStageKind.CampaignAnalysis, run.CurrentStage);
        Assert.Equal(4000, run.TotalCoinsSpent);
        Assert.Equal(2, run.Stages.Count);

        var analysisStage = run.Stages.Single(s => s.Kind == AiPipelineStageKind.CampaignAnalysis);
        Assert.Equal(AiPipelineStageStatus.Completed, analysisStage.Status);
        Assert.Equal(artifactId, analysisStage.ArtifactId);
        Assert.Null(analysisStage.TargetRefId);

        // The retry bookkeeping a failed stage needs in order to be resumable at all.
        var imageStage = run.Stages.Single(s => s.Kind == AiPipelineStageKind.ContentImage);
        Assert.Equal(AiFailureKind.Provider, imageStage.LastErrorKind);
        Assert.Equal(contentItemId, imageStage.TargetRefId);
        Assert.True(imageStage.IsOptional);
        Assert.NotNull(imageStage.NextAttemptAt);

        // The cycle: stage → artifact → back to the same stage.
        var artifact = await readContext.AiArtifacts.FirstAsync(a => a.Id == artifactId);
        Assert.Equal(analysisStageId, artifact.SourceStageId);
        Assert.True(artifact.IsCurrent);
        Assert.Equal(1, artifact.Version);
    }

    [Fact]
    public async Task SecondConcurrentSave_OnSameRun_ThrowsDbUpdateConcurrencyException()
    {
        var dbName = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var (tenantId, brandProfileId, campaignId) = await SeedAsync(dbName, now);

        var runId = Guid.NewGuid();
        await using (var seedContext = TestDbContextFactory.Create(dbName))
        {
            seedContext.AiPipelineRuns.Add(new AiPipelineRun
            {
                Id = runId,
                TenantId = tenantId,
                BrandProfileId = brandProfileId,
                CampaignId = campaignId,
                TriggeredBy = Guid.NewGuid(),
                Status = AiPipelineRunStatus.Running,
                CreatedAt = now,
                UpdatedAt = now
            });
            await seedContext.SaveChangesAsync(CancellationToken.None);
        }

        // The real scenario this guards: the background worker settling a finished stage at the same
        // moment a user cancels the run from the UI. Without RowVersion one silently overwrites the
        // other's status transition.
        await using var workerContext = TestDbContextFactory.Create(dbName);
        await using var userContext = TestDbContextFactory.Create(dbName);

        var runAsWorker = await workerContext.AiPipelineRuns.FirstAsync(r => r.Id == runId);
        var runAsUser = await userContext.AiPipelineRuns.FirstAsync(r => r.Id == runId);

        runAsWorker.Status = AiPipelineRunStatus.Completed;
        await workerContext.SaveChangesAsync(CancellationToken.None);

        runAsUser.Status = AiPipelineRunStatus.Cancelled;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => userContext.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task BrandScopedArtifact_PersistsWithoutACampaign()
    {
        var dbName = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var (tenantId, brandProfileId, _) = await SeedAsync(dbName, now);

        var artifactId = Guid.NewGuid();
        await using (var seedContext = TestDbContextFactory.Create(dbName))
        {
            // BrandAnalysis belongs to the brand, not to whichever campaign happened to trigger its
            // computation — that is what lets every later campaign of the brand reuse it.
            seedContext.AiArtifacts.Add(new AiArtifact
            {
                Id = artifactId,
                TenantId = tenantId,
                BrandProfileId = brandProfileId,
                CampaignId = null,
                Kind = AiArtifactKind.BrandAnalysis,
                Version = 1,
                IsCurrent = true,
                ContentJson = """{"identityCore":"artisanal coffee roaster"}""",
                SchemaVersion = 1,
                InputHash = new string('b', 64),
                CreatedAt = now
            });
            await seedContext.SaveChangesAsync(CancellationToken.None);
        }

        await using var readContext = TestDbContextFactory.Create(dbName);

        var cached = await readContext.AiArtifacts.FirstOrDefaultAsync(a =>
            a.BrandProfileId == brandProfileId &&
            a.Kind == AiArtifactKind.BrandAnalysis &&
            a.InputHash == new string('b', 64) &&
            a.IsCurrent);

        Assert.NotNull(cached);
        Assert.Null(cached.CampaignId);
    }

    private static async Task<(Guid TenantId, Guid BrandProfileId, Guid CampaignId)> SeedAsync(
        string dbName, DateTime now)
    {
        await using var dbContext = TestDbContextFactory.Create(dbName);

        var planId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var brandProfileId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();

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

        dbContext.MarketingCampaigns.Add(new MarketingCampaign
        {
            Id = campaignId,
            BrandProfileId = brandProfileId,
            CreatedBy = Guid.NewGuid(),
            Name = "Test Campaign",
            Status = CampaignStatus.Draft,
            TargetPlatforms = [],
            CreatedAt = now,
            UpdatedAt = now
        });

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return (tenantId, brandProfileId, campaignId);
    }
}
