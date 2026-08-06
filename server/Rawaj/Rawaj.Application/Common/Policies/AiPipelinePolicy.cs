using System.Text.Json;
using Rawaj.Domain.Entities.AiOperations;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Policies;

/// <summary>
/// One stage's fixed properties: where it sits in the graph, what it waits for, and how hard to try.
/// </summary>
/// <param name="DependsOn">Stages that must reach a successful terminal state first. A dependency is
/// satisfied by <see cref="AiPipelineStageStatus.Completed"/> <b>or</b>
/// <see cref="AiPipelineStageStatus.Skipped"/> — that equivalence is what lets an optional stage fail
/// permanently without stalling everything behind it.</param>
/// <param name="IsOptional">Whether exhausting the retries is survivable. Optional stages become
/// Skipped and the run carries on; required stages fail the run.</param>
/// <param name="FansOut">Whether a run holds several rows of this kind (one per target) rather than
/// exactly one.</param>
/// <param name="RequiresHuman">Whether this stage is completed by a person rather than a worker. The
/// orchestrator parks these instead of dispatching them.</param>
public sealed record AiPipelineStageDefinition(
    AiPipelineStageKind Kind,
    IReadOnlyList<AiPipelineStageKind> DependsOn,
    bool IsOptional,
    bool FansOut,
    bool RequiresHuman,
    int MaxAttempts,
    TimeSpan LeaseDuration)
{
    /// <summary>Display order, taken from the enum's numeric value so the two can never disagree.</summary>
    public int Ordinal => (int)Kind;
}

/// <summary>What to do with a stage that just failed.</summary>
/// <param name="Status">The status to write.</param>
/// <param name="NextAttemptAt">When it may be picked up again, for a retryable outcome.</param>
/// <param name="ConsumesAttempt">Whether this failure should count against the retry budget. A coin
/// shortfall does not: nothing was attempted and nothing is wrong with the stage.</param>
public sealed record StageFailureOutcome(
    AiPipelineStageStatus Status,
    DateTime? NextAttemptAt,
    bool ConsumesAttempt);

/// <summary>
/// The AI pipeline's graph and transition rules, as pure functions over stage rows — no database, no
/// MediatR, no provider. Everything the orchestrator and the background worker decide comes from
/// here, so the ordering rules can be tested exhaustively without running anything.
///
/// This deliberately replaces logic that currently lives in the browser: the strategy pipeline is
/// sequenced today by hand-chained subscribe() callbacks in onboarding-plan-approval.ts, which means
/// a closed tab strands a campaign with coins spent and no artifact, and the retry rules exist only
/// as ad-hoc component state.
///
/// See docs/AI_PIPELINE.md §3 (the graph) and §5–§6 (state machine and retries).
/// </summary>
public static class AiPipelinePolicy
{
    private static readonly TimeSpan DefaultLease = TimeSpan.FromMinutes(5);

    /// <summary>Backoff schedule, indexed by attempts already made. The last entry repeats if a
    /// stage somehow gets more attempts than this has entries.</summary>
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(8)
    ];

    /// <summary>
    /// The graph. A DAG, not a chain: stages sharing dependencies run in parallel, which is why
    /// brand and campaign analysis start together and the two research stages do too.
    /// </summary>
    public static readonly IReadOnlyList<AiPipelineStageDefinition> Graph =
    [
        // Brand-scoped and cached across campaigns; depends on nothing but the brand profile.
        new(AiPipelineStageKind.BrandAnalysis, [], false, false, false, 3, DefaultLease),

        // Independent of brand analysis — the two are the pipeline's first parallel pair.
        new(AiPipelineStageKind.CampaignAnalysis, [], false, false, false, 3, DefaultLease),

        // Optional: research "may help and may not help", per the existing product rule that a
        // Tavily failure must never block a campaign. Shorter lease — a search is not a generation.
        new(AiPipelineStageKind.MarketResearch,
            [AiPipelineStageKind.CampaignAnalysis], true, false, false, 3, TimeSpan.FromMinutes(2)),

        new(AiPipelineStageKind.CompetitorResearch,
            [AiPipelineStageKind.CampaignAnalysis], true, false, false, 3, TimeSpan.FromMinutes(2)),

        // The strategy, split three ways so a failure retries one sub-task instead of discarding the
        // whole artifact. Positioning needs the full picture; the blueprint doesn't need market
        // research; the roadmap builds on the blueprint and so waits for it.
        new(AiPipelineStageKind.StrategyPositioning,
            [
                AiPipelineStageKind.BrandAnalysis,
                AiPipelineStageKind.CampaignAnalysis,
                AiPipelineStageKind.MarketResearch,
                AiPipelineStageKind.CompetitorResearch
            ], false, false, false, 3, DefaultLease),

        new(AiPipelineStageKind.StrategyBlueprint,
            [
                AiPipelineStageKind.BrandAnalysis,
                AiPipelineStageKind.CampaignAnalysis,
                AiPipelineStageKind.CompetitorResearch
            ], false, false, false, 3, DefaultLease),

        new(AiPipelineStageKind.StrategyRoadmap,
            [
                AiPipelineStageKind.CampaignAnalysis,
                AiPipelineStageKind.MarketResearch,
                AiPipelineStageKind.StrategyBlueprint
            ], false, false, false, 3, DefaultLease),

        // Pure composition and schema validation, no provider call — hence the short lease. Two
        // attempts rather than three: if the same three sub-artifacts fail to assemble twice, a
        // third identical attempt will not behave differently.
        new(AiPipelineStageKind.StrategyAssemble,
            [
                AiPipelineStageKind.StrategyPositioning,
                AiPipelineStageKind.StrategyBlueprint,
                AiPipelineStageKind.StrategyRoadmap
            ], false, false, false, 2, TimeSpan.FromMinutes(1)),

        // Waits on a person. Never dispatched to a worker, never retried, never leased.
        new(AiPipelineStageKind.HumanApproval,
            [AiPipelineStageKind.StrategyAssemble], false, false, true, 1, TimeSpan.Zero),

        new(AiPipelineStageKind.ContentPlan,
            [AiPipelineStageKind.HumanApproval], false, false, false, 3, DefaultLease),

        // One row per generated post. Optional in the same sense as today: a post whose image can't
        // be produced gets the placeholder asset rather than failing the batch, because a ContentItem
        // must never end up with no image at all.
        new(AiPipelineStageKind.ContentImage,
            [AiPipelineStageKind.ContentPlan], true, true, false, 3, TimeSpan.FromMinutes(3))
    ];

    private static readonly Dictionary<AiPipelineStageKind, AiPipelineStageDefinition> ByKind =
        Graph.ToDictionary(d => d.Kind);

    public static AiPipelineStageDefinition Definition(AiPipelineStageKind kind) => ByKind[kind];

    /// <summary>The external provider a stage's call counts against, for
    /// <c>IAiProviderConcurrencyLimiter</c>. Null for a stage that calls no provider at all
    /// (<c>StrategyAssemble</c> is pure composition, <c>HumanApproval</c> waits on a person) — those
    /// need no throttling because they cost nothing to run concurrently without limit.
    ///
    /// <para>The two research stages call Tavily and then Groq; Tavily is the one worth gating here,
    /// since it is the externally rate-limited step and the Groq synthesis call that follows it is
    /// already covered by whatever limit the text-generation stages share.</para></summary>
    public static string? Provider(AiPipelineStageKind kind) => kind switch
    {
        AiPipelineStageKind.BrandAnalysis => "Groq",
        AiPipelineStageKind.CampaignAnalysis => "Groq",
        AiPipelineStageKind.MarketResearch => "Tavily",
        AiPipelineStageKind.CompetitorResearch => "Tavily",
        AiPipelineStageKind.StrategyPositioning => "Groq",
        AiPipelineStageKind.StrategyBlueprint => "Groq",
        AiPipelineStageKind.StrategyRoadmap => "Groq",
        AiPipelineStageKind.ContentPlan => "Groq",
        AiPipelineStageKind.ContentImage => "HuggingFace",
        _ => null
    };

    /// <summary>The stages a fresh run starts with. Fan-out kinds are excluded: their rows can only
    /// be created once the stage that decides how many are needed has produced its output.</summary>
    public static IReadOnlyList<AiPipelineStageDefinition> InitialStages() =>
        [.. Graph.Where(d => !d.FansOut)];

    public static bool IsTerminal(AiPipelineStageStatus status) =>
        status is AiPipelineStageStatus.Completed
            or AiPipelineStageStatus.Skipped
            or AiPipelineStageStatus.Failed;

    /// <summary>Terminal <i>and</i> successful enough for dependents to proceed. Skipped counts:
    /// an optional stage that gave up has still finished its part of the graph.</summary>
    public static bool SatisfiesDependency(AiPipelineStageStatus status) =>
        status is AiPipelineStageStatus.Completed or AiPipelineStageStatus.Skipped;

    /// <summary>
    /// The stages that may be acted on right now: pending, past their backoff, and with every
    /// dependency satisfied.
    ///
    /// Note this includes <see cref="AiPipelineStageKind.HumanApproval"/> when its turn comes.
    /// "Runnable" means "the graph no longer blocks it", not "dispatch it to a worker" — the caller
    /// checks <see cref="AiPipelineStageDefinition.RequiresHuman"/> and parks those instead.
    /// </summary>
    public static IReadOnlyList<AiPipelineStage> GetRunnableStages(
        IReadOnlyCollection<AiPipelineStage> stages, DateTime utcNow)
    {
        var byKind = stages.GroupBy(s => s.Kind).ToDictionary(g => g.Key, g => g.ToList());

        return
        [
            .. stages
                .Where(s => s.Status == AiPipelineStageStatus.Pending)
                .Where(s => s.NextAttemptAt is null || s.NextAttemptAt <= utcNow)
                .Where(s => DependenciesSatisfied(s.Kind, byKind))
                .OrderBy(s => Definition(s.Kind).Ordinal)
                .ThenBy(s => s.CreatedAt)
        ];
    }

    private static bool DependenciesSatisfied(
        AiPipelineStageKind kind, Dictionary<AiPipelineStageKind, List<AiPipelineStage>> byKind)
    {
        foreach (var dependency in Definition(kind).DependsOn)
        {
            // A dependency with no row at all is not satisfied. This matters for single-stage
            // invocation, where a caller could otherwise ask for the strategy of a run that never
            // researched anything and get one built on nothing.
            if (!byKind.TryGetValue(dependency, out var rows) || rows.Count == 0)
            {
                return false;
            }

            // Every row, not just one: the content-image fan-out is only done when all its posts are.
            if (rows.Any(r => !SatisfiesDependency(r.Status)))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The run's status implied by its stages. Ordering matters here: a required failure outranks
    /// everything, because a run that cannot finish should not present itself as merely waiting.
    /// </summary>
    public static AiPipelineRunStatus EvaluateRunStatus(IReadOnlyCollection<AiPipelineStage> stages)
    {
        if (stages.Count == 0)
        {
            return AiPipelineRunStatus.Pending;
        }

        if (stages.Any(s => s.Status == AiPipelineStageStatus.Failed && !Definition(s.Kind).IsOptional))
        {
            return AiPipelineRunStatus.Failed;
        }

        if (stages.All(s => SatisfiesDependency(s.Status)))
        {
            return AiPipelineRunStatus.Completed;
        }

        if (stages.Any(s => s.Status == AiPipelineStageStatus.AwaitingApproval))
        {
            return AiPipelineRunStatus.AwaitingApproval;
        }

        // Parked on the tenant's wallet rather than on anything the pipeline can fix itself. Checked
        // after approval so a run that is both waiting for a person and short of coins reports the
        // thing the person can act on.
        if (stages.Any(s => s.Status == AiPipelineStageStatus.Pending && s.LastErrorKind == AiFailureKind.Coins))
        {
            return AiPipelineRunStatus.AwaitingCoins;
        }

        return AiPipelineRunStatus.Running;
    }

    /// <summary>
    /// What a failure means for the stage that just had one.
    ///
    /// A coin shortfall is deliberately not a retry: nothing was attempted, so consuming one of the
    /// three attempts would let a temporarily empty wallet permanently fail a run that a top-up
    /// would have fixed.
    /// </summary>
    public static StageFailureOutcome ResolveFailure(
        AiPipelineStage stage, AiFailureKind failureKind, DateTime utcNow)
    {
        if (failureKind == AiFailureKind.Coins)
        {
            return new StageFailureOutcome(AiPipelineStageStatus.Pending, null, ConsumesAttempt: false);
        }

        var definition = Definition(stage.Kind);
        var attemptsAfterThis = stage.Attempts + 1;

        if (attemptsAfterThis >= definition.MaxAttempts)
        {
            return new StageFailureOutcome(
                definition.IsOptional ? AiPipelineStageStatus.Skipped : AiPipelineStageStatus.Failed,
                null,
                ConsumesAttempt: true);
        }

        return new StageFailureOutcome(
            AiPipelineStageStatus.Pending,
            utcNow.Add(GetRetryDelay(attemptsAfterThis)),
            ConsumesAttempt: true);
    }

    public static TimeSpan GetRetryDelay(int attemptsMade)
    {
        var index = Math.Clamp(attemptsMade - 1, 0, RetryDelays.Length - 1);
        return RetryDelays[index];
    }

    /// <summary>
    /// Whether a stage's next attempt should carry a "your last response was not valid JSON" repair
    /// instruction. Only worth doing once — a model that ignores the schema twice is not going to be
    /// argued into it a third time, and each attempt costs a real call.
    /// </summary>
    public static bool ShouldRepairPrompt(AiPipelineStage stage) =>
        stage.Attempts == 1 && stage.LastErrorKind is AiFailureKind.Parse or AiFailureKind.Validation;

    /// <summary>
    /// Classifies an exception thrown while executing a stage. Determines the retry policy, so
    /// getting it wrong is expensive in both directions: retrying an unfixable failure burns the
    /// tenant's provider quota, and giving up on a transient one loses work that would have
    /// succeeded.
    /// </summary>
    public static AiFailureKind ClassifyFailure(Exception exception) => exception switch
    {
        JsonException => AiFailureKind.Parse,
        TaskCanceledException => AiFailureKind.Provider,
        TimeoutException => AiFailureKind.Provider,
        HttpRequestException => AiFailureKind.Provider,
        _ => AiFailureKind.Unknown
    };

    /// <summary>
    /// Classifies a provider's own error message, for the services that report failure by returning
    /// a result rather than throwing (which all three of ours do).
    /// </summary>
    public static AiFailureKind ClassifyProviderError(string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return AiFailureKind.Unknown;
        }

        var message = errorMessage.ToLowerInvariant();

        // Quota and auth rejections are the ones worth separating: they exhaust every rotated key
        // identically, so a fast retry just burns through the list again for nothing.
        if (message.Contains("quota") || message.Contains("rate limit") || message.Contains("429") ||
            message.Contains("insufficient_quota") || message.Contains("402") || message.Contains("401"))
        {
            return AiFailureKind.Quota;
        }

        if (message.Contains("timeout") || message.Contains("timed out") ||
            message.Contains("503") || message.Contains("502") || message.Contains("500"))
        {
            return AiFailureKind.Provider;
        }

        return AiFailureKind.Unknown;
    }
}
