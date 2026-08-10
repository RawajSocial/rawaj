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
/// The three strategy sub-stages and the pure-composition assembler that follows them — the split
/// that replaces the single "write the whole strategy" prompt, so a bad blueprint retries only the
/// blueprint instead of discarding a 12,000-coin artifact.
/// </summary>
public class StrategyExecutorTests
{
    private const string ValidCampaignAnalysis = """
        {"objectiveClassification":"وعي","audienceSegments":[{"name":"طلاب","traits":["شباب"],"motivations":["سعر"]}],
         "successCriteria":["وصول"],"constraints":["ميزانية"],
         "researchQueries":{"market":["سوق القهوة"],"competitor":["منافسين القهوة الرياض"]}}
        """;

    private const string ValidPositioning = """
        {"businessAndMarketAnalysis":"تحليل السوق","brandStrategy":"استراتيجية العلامة","marketingStrategy":"استراتيجية التسويق"}
        """;

    private const string ValidBlueprint = """
        {"campaignBlueprint":{"pillars":["جودة"],"keyThemes":["قهوة"],
         "postingCadence":{"instagram":"3 posts/week"},"contentMix":{"image":"70%"},
         "recommendedPlatforms":["Instagram"]}}
        """;

    private const string ValidRoadmap = """
        {"executiveSummary":"ملخص تنفيذي","contentProductionPlan":"خطة الإنتاج","executionRoadmap":"خطة التنفيذ",
         "aiRecommendations":["أتمتة الجدولة"]}
        """;

    // ── StrategyPositioning ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Positioning_Succeeds_AndCarriesTheCampaignAnalysisForward()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var textService = TextServiceReturning(ValidPositioning);
        var executor = new StrategyPositioningExecutor(dbContext, textService, new PromptTemplateProvider());

        var context = world.Context(
            AiPipelineStageKind.StrategyPositioning,
            inputs: new Dictionary<AiArtifactKind, string> { [AiArtifactKind.CampaignAnalysis] = ValidCampaignAnalysis });

        var result = await executor.ExecuteAsync(context, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(AiArtifactKind.StrategyPositioning, result.ArtifactKind);
        // "طلاب" lives in audienceSegments, one of the fields StrategyPrompt.BuildPositioning keeps;
        // "سوق القهوة" lives only in researchQueries, which is intentionally trimmed out here — that
        // field is only ever needed by the research stages, not by strategy generation.
        await textService.Received(1).GenerateTextAsync(
            Arg.Is<string>(p => p != null && p.Contains("طلاب") && p.Contains("treat it as established")),
            Arg.Any<CancellationToken>(),
            Arg.Is<AiTextGenerationOptions?>(o => o != null && o.TaskName == "Strategy"));
    }

    [Fact]
    public async Task Positioning_RejectsOutputMissingRequiredFields()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var executor = new StrategyPositioningExecutor(
            dbContext, TextServiceReturning("""{"brandStrategy":"..."}"""), new PromptTemplateProvider());

        var result = await executor.ExecuteAsync(
            world.Context(AiPipelineStageKind.StrategyPositioning), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(AiFailureKind.Validation, result.FailureKind);
        Assert.Contains("businessAndMarketAnalysis", result.ErrorMessage);
    }

    // ── StrategyBlueprint ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Blueprint_Succeeds_AndDoesNotDependOnMarketResearch()
    {
        // The graph deliberately omits MarketResearch from this stage's dependencies — cadence and
        // platform mix follow from the campaign's constraints and competitors, not demand trends.
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var textService = TextServiceReturning(ValidBlueprint);
        var executor = new StrategyBlueprintExecutor(dbContext, textService, new PromptTemplateProvider());

        var context = world.Context(
            AiPipelineStageKind.StrategyBlueprint,
            inputs: new Dictionary<AiArtifactKind, string> { [AiArtifactKind.CampaignAnalysis] = ValidCampaignAnalysis });

        var result = await executor.ExecuteAsync(context, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(AiArtifactKind.StrategyBlueprint, result.ArtifactKind);
    }

    [Fact]
    public async Task Blueprint_RejectsOutputMissingCampaignBlueprint()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var executor = new StrategyBlueprintExecutor(
            dbContext, TextServiceReturning("""{"somethingElse":true}"""), new PromptTemplateProvider());

        var result = await executor.ExecuteAsync(
            world.Context(AiPipelineStageKind.StrategyBlueprint), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(AiFailureKind.Validation, result.FailureKind);
    }

    // ── StrategyRoadmap ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Roadmap_Succeeds_AndBuildsOnTheBlueprint()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var world = await SeedAsync(dbContext);
        var textService = TextServiceReturning(ValidRoadmap);
        var executor = new StrategyRoadmapExecutor(dbContext, textService, new PromptTemplateProvider());

        var context = world.Context(
            AiPipelineStageKind.StrategyRoadmap,
            inputs: new Dictionary<AiArtifactKind, string>
            {
                [AiArtifactKind.CampaignAnalysis] = ValidCampaignAnalysis,
                [AiArtifactKind.StrategyBlueprint] = ValidBlueprint
            });

        var result = await executor.ExecuteAsync(context, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(AiArtifactKind.StrategyRoadmap, result.ArtifactKind);
        await textService.Received(1).GenerateTextAsync(
            Arg.Is<string>(p => p != null && p.Contains("do not contradict it") && p.Contains("جودة")),
            Arg.Any<CancellationToken>(), Arg.Any<AiTextGenerationOptions>());
    }

    // ── StrategyAssemble: pure composition, no model call ──────────────────────────────────────

    [Fact]
    public async Task Assemble_MergesTheThreePartsIntoOneStrategy_WithNoModelCall()
    {
        var world = await SeedAsync(TestDbContextFactory.Create());
        var executor = new StrategyAssembleExecutor();

        var context = world.Context(
            AiPipelineStageKind.StrategyAssemble,
            inputs: new Dictionary<AiArtifactKind, string>
            {
                [AiArtifactKind.StrategyPositioning] = ValidPositioning,
                [AiArtifactKind.StrategyBlueprint] = ValidBlueprint,
                [AiArtifactKind.StrategyRoadmap] = ValidRoadmap
            });

        var result = await executor.ExecuteAsync(context, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(AiArtifactKind.Strategy, result.ArtifactKind);
        Assert.Null(result.AiJobIds);

        Assert.Contains("executiveSummary", result.ArtifactJson);
        Assert.Contains("businessAndMarketAnalysis", result.ArtifactJson);
        Assert.Contains("brandStrategy", result.ArtifactJson);
        Assert.Contains("marketingStrategy", result.ArtifactJson);
        Assert.Contains("campaignBlueprint", result.ArtifactJson);
        Assert.Contains("contentProductionPlan", result.ArtifactJson);
        Assert.Contains("executionRoadmap", result.ArtifactJson);
        Assert.Contains("aiRecommendations", result.ArtifactJson);
    }

    [Fact]
    public async Task Assemble_Fails_WhenAPartIsMissing()
    {
        var world = await SeedAsync(TestDbContextFactory.Create());
        var executor = new StrategyAssembleExecutor();

        var context = world.Context(
            AiPipelineStageKind.StrategyAssemble,
            inputs: new Dictionary<AiArtifactKind, string>
            {
                [AiArtifactKind.StrategyPositioning] = ValidPositioning,
                [AiArtifactKind.StrategyBlueprint] = ValidBlueprint
            });

        var result = await executor.ExecuteAsync(context, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(AiFailureKind.Validation, result.FailureKind);
    }

    [Fact]
    public async Task Assemble_Fails_WhenAPartIsMalformedJson()
    {
        var world = await SeedAsync(TestDbContextFactory.Create());
        var executor = new StrategyAssembleExecutor();

        var context = world.Context(
            AiPipelineStageKind.StrategyAssemble,
            inputs: new Dictionary<AiArtifactKind, string>
            {
                [AiArtifactKind.StrategyPositioning] = "{not json",
                [AiArtifactKind.StrategyBlueprint] = ValidBlueprint,
                [AiArtifactKind.StrategyRoadmap] = ValidRoadmap
            });

        var result = await executor.ExecuteAsync(context, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(AiFailureKind.Parse, result.FailureKind);
    }

    [Fact]
    public async Task Assemble_Fails_WhenTheMergedResultIsMissingARequiredField()
    {
        // The blueprint's contribution is required by the Strategy schema; drop it and assembly
        // should fail exactly as if a generation stage had produced an incomplete artifact.
        var world = await SeedAsync(TestDbContextFactory.Create());
        var executor = new StrategyAssembleExecutor();

        var context = world.Context(
            AiPipelineStageKind.StrategyAssemble,
            inputs: new Dictionary<AiArtifactKind, string>
            {
                [AiArtifactKind.StrategyPositioning] = ValidPositioning,
                [AiArtifactKind.StrategyBlueprint] = """{"somethingElse":true}""",
                [AiArtifactKind.StrategyRoadmap] = ValidRoadmap
            });

        var result = await executor.ExecuteAsync(context, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(AiFailureKind.Validation, result.FailureKind);
        Assert.Contains("campaignBlueprint", result.ErrorMessage);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────

    private static IAiTextGenerationService TextServiceReturning(string text)
    {
        var service = Substitute.For<IAiTextGenerationService>();
        service.GenerateTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<AiTextGenerationOptions>())
            .Returns(AiTextGenerationResult.Success(text, 500, "test-model", 1234));
        return service;
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
