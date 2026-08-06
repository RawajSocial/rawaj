using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Services;
using Rawaj.Application.Features.AiPipeline.Executors;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Domain.ValueObjects;
using Rawaj.Persistence;
using Xunit;

namespace Rawaj.Application.Tests.Features.AiPipeline;

/// <summary>
/// The brand and campaign analysis stages, against a substituted text provider — so the executor's
/// own behaviour (caching, validation, failure classification, provider logging) is what is being
/// tested rather than a model's output.
/// </summary>
public class AnalysisExecutorTests
{
    private const string ValidBrandAnalysis = """
        {"businessSummary":"مقهى مختص","identityCore":"قهوة مختصة",
         "voiceProfile":{"tone":"ودود","register":"غير رسمي","avoid":["مبالغة"]},
         "valuePropositions":["جودة"],"differentiators":["تحميص محلي"],"contentGuardrails":["لا ادعاءات صحية"]}
        """;

    private const string ValidCampaignAnalysis = """
        {"objectiveClassification":"وعي","audienceSegments":[{"name":"طلاب","traits":["شباب"],"motivations":["سعر"]}],
         "successCriteria":["وصول"],"constraints":["ميزانية"],
         "researchQueries":{"market":["سوق القهوة"],"competitor":["منافسين القهوة الرياض"]}}
        """;

    // ── Brand analysis: the cache ───────────────────────────────────────────────────────────

    [Fact]
    public async Task BrandAnalysis_ReusesAnExistingArtifact_WithoutCallingTheModel()
    {
        // The point of making this stage brand-scoped: the second campaign for a brand, and every
        // one after it, gets its analysis instantly and for free.
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var textService = TextServiceReturning(ValidBrandAnalysis);
        var store = new PipelineArtifactStore(dbContext);

        var executor = new BrandAnalysisExecutor(dbContext, textService, new PromptTemplateProvider(), store);

        var first = await executor.ExecuteAsync(world.Context(AiPipelineStageKind.BrandAnalysis), CancellationToken.None);
        await PersistArtifactAsync(dbContext, store, world, first);

        var second = await executor.ExecuteAsync(world.Context(AiPipelineStageKind.BrandAnalysis), CancellationToken.None);

        Assert.True(second.Succeeded);
        Assert.NotNull(second.ReusedArtifactId);
        Assert.Null(second.ArtifactJson);

        // One model call in total, for two executions.
        await textService.Received(1).GenerateTextAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<AiTextGenerationOptions>());
    }

    [Fact]
    public async Task BrandAnalysis_MissesTheCache_WhenIdentityFieldsChange()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var textService = TextServiceReturning(ValidBrandAnalysis);
        var store = new PipelineArtifactStore(dbContext);
        var executor = new BrandAnalysisExecutor(dbContext, textService, new PromptTemplateProvider(), store);

        var first = await executor.ExecuteAsync(world.Context(AiPipelineStageKind.BrandAnalysis), CancellationToken.None);
        await PersistArtifactAsync(dbContext, store, world, first);

        world.Brand.BrandInfo = new BrandInfo { Industry = "something entirely different" };

        var second = await executor.ExecuteAsync(world.Context(AiPipelineStageKind.BrandAnalysis), CancellationToken.None);

        Assert.Null(second.ReusedArtifactId);
        Assert.NotNull(second.ArtifactJson);
        await textService.Received(2).GenerateTextAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<AiTextGenerationOptions>());
    }

    [Fact]
    public async Task BrandAnalysis_KeepsTheCache_WhenAnIrrelevantFieldChanges()
    {
        // An edit the analysis never read must not throw away a good artifact and charge for another.
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var textService = TextServiceReturning(ValidBrandAnalysis);
        var store = new PipelineArtifactStore(dbContext);
        var executor = new BrandAnalysisExecutor(dbContext, textService, new PromptTemplateProvider(), store);

        var first = await executor.ExecuteAsync(world.Context(AiPipelineStageKind.BrandAnalysis), CancellationToken.None);
        await PersistArtifactAsync(dbContext, store, world, first);

        world.Brand.UpdatedAt = DateTime.UtcNow.AddDays(1);
        world.Brand.Status = BrandProfileStatus.Archived;

        var second = await executor.ExecuteAsync(world.Context(AiPipelineStageKind.BrandAnalysis), CancellationToken.None);

        Assert.NotNull(second.ReusedArtifactId);
    }

    [Fact]
    public async Task BrandAnalysis_PointsTheBrandAtItsCurrentArtifact_OnACacheHit()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var store = new PipelineArtifactStore(dbContext);
        var executor = new BrandAnalysisExecutor(
            dbContext, TextServiceReturning(ValidBrandAnalysis), new PromptTemplateProvider(), store);

        var first = await executor.ExecuteAsync(world.Context(AiPipelineStageKind.BrandAnalysis), CancellationToken.None);
        var artifact = await PersistArtifactAsync(dbContext, store, world, first);

        await executor.ExecuteAsync(world.Context(AiPipelineStageKind.BrandAnalysis), CancellationToken.None);

        Assert.Equal(artifact!.Id, world.Brand.CurrentBrandAnalysisArtifactId);
    }

    // ── Shared executor behaviour ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Analysis_RecordsTheProviderCall_WithModelAndLatency()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var executor = new CampaignAnalysisExecutor(
            dbContext, TextServiceReturning(ValidCampaignAnalysis), new PromptTemplateProvider());

        var result = await executor.ExecuteAsync(world.Context(AiPipelineStageKind.CampaignAnalysis), CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        Assert.True(result.Succeeded);

        var job = await dbContext.AiJobs.SingleAsync();
        Assert.Equal(world.Tenant.Id, job.TenantId);
        Assert.Equal(world.Stage.Id, job.PipelineStageId);
        Assert.Equal("Groq", job.Provider);
        Assert.Equal("test-model", job.Model);
        Assert.Equal(1234, job.LatencyMs);
        Assert.False(string.IsNullOrWhiteSpace(job.PromptHash));
    }

    [Fact]
    public async Task Analysis_LogsTheFailedCall_Too()
    {
        // A failure that leaves no trace is a failure nobody can diagnose or bill for correctly.
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var textService = Substitute.For<IAiTextGenerationService>();
        textService.GenerateTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<AiTextGenerationOptions>())
            .Returns(AiTextGenerationResult.Failure("Rate limit reached for model", "test-model", 90));

        var executor = new CampaignAnalysisExecutor(dbContext, textService, new PromptTemplateProvider());

        var result = await executor.ExecuteAsync(world.Context(AiPipelineStageKind.CampaignAnalysis), CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        Assert.False(result.Succeeded);
        // Classified, not assumed transient — a quota rejection earns a long backoff.
        Assert.Equal(AiFailureKind.Quota, result.FailureKind);
        Assert.Equal(AiJobStatus.Failed, (await dbContext.AiJobs.SingleAsync()).Status);
    }

    [Fact]
    public async Task Analysis_RejectsOutputMissingRequiredFields()
    {
        // Storing this would pass every "does an analysis exist" check while being useless to every
        // stage downstream.
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var executor = new CampaignAnalysisExecutor(
            dbContext, TextServiceReturning("""{"objectiveClassification":"وعي"}"""), new PromptTemplateProvider());

        var result = await executor.ExecuteAsync(world.Context(AiPipelineStageKind.CampaignAnalysis), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(AiFailureKind.Validation, result.FailureKind);
        Assert.Contains("researchQueries", result.ErrorMessage);
    }

    [Fact]
    public async Task Analysis_RecoversAFencedResponse()
    {
        // Models wrap JSON in markdown despite being told not to, and the call has already been paid
        // for by the time we find out.
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var fenced = "```json\n" + ValidCampaignAnalysis + "\n```";
        var executor = new CampaignAnalysisExecutor(
            dbContext, TextServiceReturning(fenced), new PromptTemplateProvider());

        var result = await executor.ExecuteAsync(world.Context(AiPipelineStageKind.CampaignAnalysis), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.DoesNotContain("```", result.ArtifactJson);
    }

    [Fact]
    public async Task Analysis_UsesTheStagesConfiguredGenerationSettings()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var textService = TextServiceReturning(ValidCampaignAnalysis);
        var executor = new CampaignAnalysisExecutor(dbContext, textService, new PromptTemplateProvider());

        await executor.ExecuteAsync(world.Context(AiPipelineStageKind.CampaignAnalysis), CancellationToken.None);

        await textService.Received(1).GenerateTextAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>(),
            Arg.Is<AiTextGenerationOptions?>(o => o != null && o.JsonMode && o.TaskName == "CampaignAnalysis"));
    }

    [Fact]
    public async Task Analysis_AddsTheRepairInstruction_OnlyWhenRetryingAShapeFailure()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var textService = TextServiceReturning(ValidCampaignAnalysis);
        var executor = new CampaignAnalysisExecutor(dbContext, textService, new PromptTemplateProvider());

        await executor.ExecuteAsync(world.Context(AiPipelineStageKind.CampaignAnalysis), CancellationToken.None);
        await textService.Received(1).GenerateTextAsync(
            Arg.Is<string>(p => p != null && !p.Contains("could not be parsed as JSON")),
            Arg.Any<CancellationToken>(), Arg.Any<AiTextGenerationOptions>());

        await executor.ExecuteAsync(
            world.Context(AiPipelineStageKind.CampaignAnalysis, repairPrompt: true), CancellationToken.None);
        await textService.Received(1).GenerateTextAsync(
            Arg.Is<string>(p => p != null && p.Contains("could not be parsed as JSON")),
            Arg.Any<CancellationToken>(), Arg.Any<AiTextGenerationOptions>());
    }

    [Fact]
    public async Task CampaignAnalysis_PassesTheBrandAnalysisThrough_RatherThanRestatingBrandFields()
    {
        // The grounding chain: each stage is fed the previous stage's confirmed output.
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var textService = TextServiceReturning(ValidCampaignAnalysis);
        var executor = new CampaignAnalysisExecutor(dbContext, textService, new PromptTemplateProvider());

        var context = world.Context(
            AiPipelineStageKind.CampaignAnalysis,
            inputs: new Dictionary<AiArtifactKind, string> { [AiArtifactKind.BrandAnalysis] = ValidBrandAnalysis });

        await executor.ExecuteAsync(context, CancellationToken.None);

        await textService.Received(1).GenerateTextAsync(
            Arg.Is<string>(p => p != null && p.Contains("قهوة مختصة") && p.Contains("do not restate it")),
            Arg.Any<CancellationToken>(), Arg.Any<AiTextGenerationOptions>());
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────

    private static IAiTextGenerationService TextServiceReturning(string text)
    {
        var service = Substitute.For<IAiTextGenerationService>();
        service.GenerateTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<AiTextGenerationOptions>())
            .Returns(AiTextGenerationResult.Success(text, 500, "test-model", 1234));
        return service;
    }

    private static async Task<AiArtifact?> PersistArtifactAsync(
        AppDbContext dbContext, PipelineArtifactStore store, World world, StageResult result)
    {
        if (result.ArtifactJson is null)
        {
            return null;
        }

        var artifact = await store.AddVersionAsync(
            world.Stage, world.Tenant.Id, world.Brand.Id, world.Campaign.Id,
            result.ArtifactKind!.Value, result.ArtifactJson, result.InputHash, CancellationToken.None);

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return artifact;
    }

    private sealed record World(Tenant Tenant, TenantBrandProfile Brand, MarketingCampaign Campaign, AiPipelineRun Run)
    {
        public AiPipelineStage Stage { get; private set; } = null!;

        public StageContext Context(
            AiPipelineStageKind kind,
            IReadOnlyDictionary<AiArtifactKind, string>? inputs = null,
            bool repairPrompt = false)
        {
            Stage = new AiPipelineStage
            {
                Id = Guid.NewGuid(),
                RunId = Run.Id,
                Kind = kind,
                Status = AiPipelineStageStatus.Running,
                CreatedAt = DateTime.UtcNow
            };

            return new StageContext(
                Run, Stage, Brand, Campaign,
                inputs ?? new Dictionary<AiArtifactKind, string>(),
                Run.TriggeredBy, TenantMemberRole.Owner, repairPrompt);
        }
    }

    private static async Task<World> SeedAsync(AppDbContext dbContext)
    {
        var now = DateTime.UtcNow;
        var planId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

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

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(), Name = "Test Tenant", Subdomain = Guid.NewGuid().ToString("N"),
            TenantType = TenantType.Business, OwnerUserId = Guid.NewGuid(), SubscriptionId = subscriptionId,
            CoinBalance = 100_000, IsActive = true, IsActivated = true, CreatedAt = now, UpdatedAt = now
        };
        dbContext.Tenants.Add(tenant);

        var brand = new TenantBrandProfile
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, Name = "Qahwa",
            Description = "Specialty coffee roaster",
            BrandInfo = new BrandInfo { Industry = "Coffee", Keywords = ["specialty", "roastery"] },
            Status = BrandProfileStatus.Active, CreatedAt = now, UpdatedAt = now
        };
        dbContext.TenantBrandProfiles.Add(brand);

        var campaign = new MarketingCampaign
        {
            Id = Guid.NewGuid(), BrandProfileId = brand.Id, CreatedBy = Guid.NewGuid(),
            Name = "Summer Push", Objective = "awareness", TargetPlatforms = ["Instagram"],
            BriefJson = """{"positioning":"premium"}""",
            Status = CampaignStatus.Draft, CreatedAt = now, UpdatedAt = now
        };
        dbContext.MarketingCampaigns.Add(campaign);

        var run = new AiPipelineRun
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BrandProfileId = brand.Id, CampaignId = campaign.Id,
            TriggeredBy = Guid.NewGuid(), Status = AiPipelineRunStatus.Running, CreatedAt = now, UpdatedAt = now
        };
        dbContext.AiPipelineRuns.Add(run);

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return new World(tenant, brand, campaign, run);
    }
}
