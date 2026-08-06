using System.Text.Json;
using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Enums;
using Xunit;

namespace Rawaj.Application.Tests.Policies;

/// <summary>
/// The pipeline graph and its transition rules. Every ordering, retry and parking decision the
/// orchestrator and the background worker make comes from AiPipelinePolicy, so this file is where
/// those decisions are actually verified — cheaply, with no database, no provider and no worker.
/// </summary>
public class AiPipelinePolicyTests
{
    private static readonly DateTime Now = new(2026, 8, 6, 12, 0, 0, DateTimeKind.Utc);

    // ── The graph itself ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Graph_CoversEveryStageKind_Exactly_Once()
    {
        var kinds = Enum.GetValues<AiPipelineStageKind>();

        Assert.Equal(kinds.Length, AiPipelinePolicy.Graph.Count);
        Assert.Equal(kinds.Length, AiPipelinePolicy.Graph.Select(d => d.Kind).Distinct().Count());
    }

    [Fact]
    public void Graph_HasNoDependencyOnAStageThatRunsLater()
    {
        // A dependency pointing forwards would be a cycle in disguise: the run would deadlock with
        // every stage waiting on something that waits on it.
        foreach (var definition in AiPipelinePolicy.Graph)
        {
            foreach (var dependency in definition.DependsOn)
            {
                Assert.True(
                    AiPipelinePolicy.Definition(dependency).Ordinal < definition.Ordinal,
                    $"{definition.Kind} depends on {dependency}, which does not run before it.");
            }
        }
    }

    [Fact]
    public void InitialStages_ExcludeFanOutKinds()
    {
        // ContentImage rows can only exist once ContentPlan has decided how many posts there are.
        var initial = AiPipelinePolicy.InitialStages();

        Assert.DoesNotContain(initial, d => d.Kind == AiPipelineStageKind.ContentImage);
        Assert.Contains(initial, d => d.Kind == AiPipelineStageKind.ContentPlan);
    }

    [Fact]
    public void OnlyResearchAndImageStages_AreOptional()
    {
        var optional = AiPipelinePolicy.Graph.Where(d => d.IsOptional).Select(d => d.Kind).ToList();

        Assert.Equal(
            [
                AiPipelineStageKind.MarketResearch,
                AiPipelineStageKind.CompetitorResearch,
                AiPipelineStageKind.ContentImage
            ],
            optional);
    }

    // ── Runnable selection ──────────────────────────────────────────────────────────────────

    [Fact]
    public void FreshRun_StartsBothAnalysisStages_InParallel()
    {
        var stages = FreshRun();

        var runnable = AiPipelinePolicy.GetRunnableStages(stages, Now);

        Assert.Equal(
            [AiPipelineStageKind.BrandAnalysis, AiPipelineStageKind.CampaignAnalysis],
            runnable.Select(s => s.Kind));
    }

    [Fact]
    public void ResearchStages_Wait_ForCampaignAnalysis_ButNotForBrandAnalysis()
    {
        var stages = FreshRun();
        Complete(stages, AiPipelineStageKind.CampaignAnalysis);

        var runnable = AiPipelinePolicy.GetRunnableStages(stages, Now).Select(s => s.Kind).ToList();

        // Brand analysis is still pending and unrelated to research — the two branches are parallel.
        Assert.Contains(AiPipelineStageKind.MarketResearch, runnable);
        Assert.Contains(AiPipelineStageKind.CompetitorResearch, runnable);
        Assert.Contains(AiPipelineStageKind.BrandAnalysis, runnable);
        Assert.DoesNotContain(AiPipelineStageKind.StrategyPositioning, runnable);
    }

    [Fact]
    public void SkippedDependency_UnblocksDependents_JustLikeCompletion()
    {
        // The whole point of optional stages: research failing permanently must not strand the
        // strategy behind it.
        var stages = FreshRun();
        Complete(stages, AiPipelineStageKind.BrandAnalysis);
        Complete(stages, AiPipelineStageKind.CampaignAnalysis);
        Skip(stages, AiPipelineStageKind.MarketResearch);
        Skip(stages, AiPipelineStageKind.CompetitorResearch);

        var runnable = AiPipelinePolicy.GetRunnableStages(stages, Now).Select(s => s.Kind).ToList();

        Assert.Contains(AiPipelineStageKind.StrategyPositioning, runnable);
        Assert.Contains(AiPipelineStageKind.StrategyBlueprint, runnable);
    }

    [Fact]
    public void StrategyRoadmap_WaitsForTheBlueprint_EvenWhenItsOtherDependenciesAreDone()
    {
        var stages = FreshRun();
        Complete(stages, AiPipelineStageKind.BrandAnalysis);
        Complete(stages, AiPipelineStageKind.CampaignAnalysis);
        Complete(stages, AiPipelineStageKind.MarketResearch);
        Complete(stages, AiPipelineStageKind.CompetitorResearch);

        var runnable = AiPipelinePolicy.GetRunnableStages(stages, Now).Select(s => s.Kind).ToList();

        Assert.Contains(AiPipelineStageKind.StrategyPositioning, runnable);
        Assert.Contains(AiPipelineStageKind.StrategyBlueprint, runnable);
        Assert.DoesNotContain(AiPipelineStageKind.StrategyRoadmap, runnable);
    }

    [Fact]
    public void StageInBackoff_IsNotRunnable_UntilItsNextAttemptTimePasses()
    {
        var stages = FreshRun();
        var brandAnalysis = Find(stages, AiPipelineStageKind.BrandAnalysis);
        brandAnalysis.Attempts = 1;
        brandAnalysis.NextAttemptAt = Now.AddMinutes(2);

        var duringBackoff = AiPipelinePolicy.GetRunnableStages(stages, Now).Select(s => s.Kind);
        var afterBackoff = AiPipelinePolicy.GetRunnableStages(stages, Now.AddMinutes(3)).Select(s => s.Kind);

        Assert.DoesNotContain(AiPipelineStageKind.BrandAnalysis, duringBackoff);
        Assert.Contains(AiPipelineStageKind.BrandAnalysis, afterBackoff);
    }

    [Fact]
    public void RunningStage_IsNotOfferedAgain()
    {
        // The claim is atomic in the database, but the policy must not hand out a stage a worker is
        // already executing — that would be a second paid provider call for the same work.
        var stages = FreshRun();
        Find(stages, AiPipelineStageKind.BrandAnalysis).Status = AiPipelineStageStatus.Running;

        var runnable = AiPipelinePolicy.GetRunnableStages(stages, Now).Select(s => s.Kind);

        Assert.DoesNotContain(AiPipelineStageKind.BrandAnalysis, runnable);
    }

    [Fact]
    public void MissingDependencyRow_BlocksAStage()
    {
        // Single-stage invocation could otherwise ask for a strategy on a run that never analysed
        // anything, and get one grounded in nothing.
        var stages = new List<AiPipelineStage> { Stage(AiPipelineStageKind.StrategyBlueprint) };

        var runnable = AiPipelinePolicy.GetRunnableStages(stages, Now);

        Assert.Empty(runnable);
    }

    [Fact]
    public void ContentImageFanOut_BlocksNothing_ButAllRowsMustFinishForTheRunToComplete()
    {
        var stages = CompletedThrough(AiPipelineStageKind.ContentPlan);
        var firstImage = Stage(AiPipelineStageKind.ContentImage, targetRefId: Guid.NewGuid());
        var secondImage = Stage(AiPipelineStageKind.ContentImage, targetRefId: Guid.NewGuid());
        stages.AddRange([firstImage, secondImage]);

        var runnable = AiPipelinePolicy.GetRunnableStages(stages, Now);

        Assert.Equal(2, runnable.Count);
        Assert.All(runnable, s => Assert.Equal(AiPipelineStageKind.ContentImage, s.Kind));

        firstImage.Status = AiPipelineStageStatus.Completed;
        Assert.Equal(AiPipelineRunStatus.Running, AiPipelinePolicy.EvaluateRunStatus(stages));

        secondImage.Status = AiPipelineStageStatus.Skipped;
        Assert.Equal(AiPipelineRunStatus.Completed, AiPipelinePolicy.EvaluateRunStatus(stages));
    }

    // ── Run status ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void RequiredStageFailure_FailsTheRun()
    {
        var stages = FreshRun();
        Find(stages, AiPipelineStageKind.CampaignAnalysis).Status = AiPipelineStageStatus.Failed;

        Assert.Equal(AiPipelineRunStatus.Failed, AiPipelinePolicy.EvaluateRunStatus(stages));
    }

    [Fact]
    public void OptionalStageSkip_DoesNotFailTheRun()
    {
        var stages = FreshRun();
        Skip(stages, AiPipelineStageKind.MarketResearch);

        Assert.NotEqual(AiPipelineRunStatus.Failed, AiPipelinePolicy.EvaluateRunStatus(stages));
    }

    [Fact]
    public void RunParksOnApproval_ThenResumes()
    {
        var stages = CompletedThrough(AiPipelineStageKind.StrategyAssemble);
        var approval = Find(stages, AiPipelineStageKind.HumanApproval);

        approval.Status = AiPipelineStageStatus.AwaitingApproval;
        Assert.Equal(AiPipelineRunStatus.AwaitingApproval, AiPipelinePolicy.EvaluateRunStatus(stages));

        approval.Status = AiPipelineStageStatus.Completed;
        Assert.Equal(AiPipelineRunStatus.Running, AiPipelinePolicy.EvaluateRunStatus(stages));
    }

    [Fact]
    public void CoinShortfall_ParksTheRun_RatherThanFailingIt()
    {
        var stages = FreshRun();
        var campaignAnalysis = Find(stages, AiPipelineStageKind.CampaignAnalysis);
        campaignAnalysis.LastErrorKind = AiFailureKind.Coins;

        Assert.Equal(AiPipelineRunStatus.AwaitingCoins, AiPipelinePolicy.EvaluateRunStatus(stages));
    }

    [Fact]
    public void RequiredFailure_OutranksApprovalAndCoins()
    {
        // A run that cannot finish must not present itself as merely waiting on the user.
        var stages = CompletedThrough(AiPipelineStageKind.StrategyAssemble);
        Find(stages, AiPipelineStageKind.HumanApproval).Status = AiPipelineStageStatus.AwaitingApproval;
        Find(stages, AiPipelineStageKind.ContentPlan).Status = AiPipelineStageStatus.Failed;

        Assert.Equal(AiPipelineRunStatus.Failed, AiPipelinePolicy.EvaluateRunStatus(stages));
    }

    [Fact]
    public void RunWithNoStages_IsPending()
    {
        Assert.Equal(AiPipelineRunStatus.Pending, AiPipelinePolicy.EvaluateRunStatus([]));
    }

    // ── Failure resolution ──────────────────────────────────────────────────────────────────

    [Fact]
    public void FirstFailure_SchedulesARetry()
    {
        var stage = Stage(AiPipelineStageKind.CampaignAnalysis);

        var outcome = AiPipelinePolicy.ResolveFailure(stage, AiFailureKind.Provider, Now);

        Assert.Equal(AiPipelineStageStatus.Pending, outcome.Status);
        Assert.Equal(Now.AddSeconds(30), outcome.NextAttemptAt);
        Assert.True(outcome.ConsumesAttempt);
    }

    [Fact]
    public void FinalFailure_OfARequiredStage_Fails()
    {
        var stage = Stage(AiPipelineStageKind.CampaignAnalysis);
        stage.Attempts = 2; // MaxAttempts is 3, so this failure is the last one.

        var outcome = AiPipelinePolicy.ResolveFailure(stage, AiFailureKind.Provider, Now);

        Assert.Equal(AiPipelineStageStatus.Failed, outcome.Status);
        Assert.Null(outcome.NextAttemptAt);
    }

    [Fact]
    public void FinalFailure_OfAnOptionalStage_Skips()
    {
        var stage = Stage(AiPipelineStageKind.MarketResearch);
        stage.Attempts = 2;

        var outcome = AiPipelinePolicy.ResolveFailure(stage, AiFailureKind.Provider, Now);

        Assert.Equal(AiPipelineStageStatus.Skipped, outcome.Status);
    }

    [Fact]
    public void CoinShortfall_ConsumesNoAttempt_AndSetsNoBackoff()
    {
        // Otherwise a briefly empty wallet could permanently fail a run that a top-up would fix.
        var stage = Stage(AiPipelineStageKind.CampaignAnalysis);
        stage.Attempts = 2;

        var outcome = AiPipelinePolicy.ResolveFailure(stage, AiFailureKind.Coins, Now);

        Assert.Equal(AiPipelineStageStatus.Pending, outcome.Status);
        Assert.False(outcome.ConsumesAttempt);
        Assert.Null(outcome.NextAttemptAt);
    }

    [Fact]
    public void RetryDelay_Escalates_ThenHolds()
    {
        Assert.Equal(TimeSpan.FromSeconds(30), AiPipelinePolicy.GetRetryDelay(1));
        Assert.Equal(TimeSpan.FromMinutes(2), AiPipelinePolicy.GetRetryDelay(2));
        Assert.Equal(TimeSpan.FromMinutes(8), AiPipelinePolicy.GetRetryDelay(3));
        Assert.Equal(TimeSpan.FromMinutes(8), AiPipelinePolicy.GetRetryDelay(9));
    }

    [Fact]
    public void PromptRepair_IsOfferedOnce_AndOnlyForShapeFailures()
    {
        var parseFailure = Stage(AiPipelineStageKind.StrategyBlueprint);
        parseFailure.Attempts = 1;
        parseFailure.LastErrorKind = AiFailureKind.Parse;
        Assert.True(AiPipelinePolicy.ShouldRepairPrompt(parseFailure));

        // A second parse failure means the model is not going to be argued into the schema, and each
        // attempt is a real paid call.
        parseFailure.Attempts = 2;
        Assert.False(AiPipelinePolicy.ShouldRepairPrompt(parseFailure));

        var providerFailure = Stage(AiPipelineStageKind.StrategyBlueprint);
        providerFailure.Attempts = 1;
        providerFailure.LastErrorKind = AiFailureKind.Provider;
        Assert.False(AiPipelinePolicy.ShouldRepairPrompt(providerFailure));
    }

    // ── Failure classification ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Rate limit reached for model", AiFailureKind.Quota)]
    [InlineData("HTTP 429 Too Many Requests", AiFailureKind.Quota)]
    [InlineData("401 Unauthorized", AiFailureKind.Quota)]
    [InlineData("The request timed out", AiFailureKind.Provider)]
    [InlineData("503 Service Unavailable", AiFailureKind.Provider)]
    [InlineData("Something else entirely", AiFailureKind.Unknown)]
    [InlineData(null, AiFailureKind.Unknown)]
    public void ProviderErrors_AreClassified(string? message, AiFailureKind expected)
    {
        Assert.Equal(expected, AiPipelinePolicy.ClassifyProviderError(message));
    }

    [Fact]
    public void Exceptions_AreClassified()
    {
        Assert.Equal(AiFailureKind.Parse, AiPipelinePolicy.ClassifyFailure(new JsonException()));
        Assert.Equal(AiFailureKind.Provider, AiPipelinePolicy.ClassifyFailure(new HttpRequestException()));
        Assert.Equal(AiFailureKind.Provider, AiPipelinePolicy.ClassifyFailure(new TaskCanceledException()));
        Assert.Equal(AiFailureKind.Unknown, AiPipelinePolicy.ClassifyFailure(new InvalidOperationException()));
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────

    private static List<AiPipelineStage> FreshRun() =>
        [.. AiPipelinePolicy.InitialStages().Select(d => Stage(d.Kind))];

    /// <summary>A run whose stages up to and including <paramref name="through"/> are Completed.</summary>
    private static List<AiPipelineStage> CompletedThrough(AiPipelineStageKind through)
    {
        var stages = FreshRun();
        var limit = AiPipelinePolicy.Definition(through).Ordinal;

        foreach (var stage in stages.Where(s => AiPipelinePolicy.Definition(s.Kind).Ordinal <= limit))
        {
            stage.Status = AiPipelineStageStatus.Completed;
        }

        return stages;
    }

    private static AiPipelineStage Stage(AiPipelineStageKind kind, Guid? targetRefId = null)
    {
        var definition = AiPipelinePolicy.Definition(kind);

        return new AiPipelineStage
        {
            Id = Guid.NewGuid(),
            RunId = Guid.Empty,
            Kind = kind,
            Ordinal = definition.Ordinal,
            Status = AiPipelineStageStatus.Pending,
            IsOptional = definition.IsOptional,
            MaxAttempts = definition.MaxAttempts,
            TargetRefId = targetRefId,
            CreatedAt = Now
        };
    }

    private static AiPipelineStage Find(List<AiPipelineStage> stages, AiPipelineStageKind kind) =>
        stages.Single(s => s.Kind == kind);

    private static void Complete(List<AiPipelineStage> stages, AiPipelineStageKind kind) =>
        Find(stages, kind).Status = AiPipelineStageStatus.Completed;

    private static void Skip(List<AiPipelineStage> stages, AiPipelineStageKind kind) =>
        Find(stages, kind).Status = AiPipelineStageStatus.Skipped;
}
