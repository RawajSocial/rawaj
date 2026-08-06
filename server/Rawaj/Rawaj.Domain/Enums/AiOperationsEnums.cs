namespace Rawaj.Domain.Enums;

public enum AiJobType
{
    MarketAnalysis,
    PlanGeneration,
    ContentGeneration,
    ImageGeneration,
    RagIndex,
    Scheduling
}

public enum AiJobStatus
{
    Queued,
    Running,
    Completed,
    Failed
}

/// <summary>
/// The stages of the campaign AI pipeline (see docs/AI_PIPELINE.md §3). Explicit numeric values
/// because these are persisted, and because the value doubles as the display ordinal — gaps of 10
/// leave room to insert a stage (e.g. a future content-moderation pass between ContentPlan and
/// ContentImage) without renumbering rows that already exist in the database.
///
/// The graph is a DAG, not a chain: stages with no dependency between them run in parallel. The
/// dependency edges themselves live in AiPipelinePolicy, not here — this enum only names the stages.
/// </summary>
public enum AiPipelineStageKind
{
    /// <summary>Brand-scoped and cached across campaigns: the same brand yields the same analysis
    /// until its profile changes, so this is computed once per brand rather than re-derived by the
    /// model on every prompt of every campaign.</summary>
    BrandAnalysis = 10,

    /// <summary>Campaign-scoped understanding of the objective, audience and constraints. Also emits
    /// the search queries that drive MarketResearch and CompetitorResearch — letting the model that
    /// just understood the campaign choose them, rather than string-concatenating brand keywords.</summary>
    CampaignAnalysis = 20,

    /// <summary>"What is happening in this market" — trends, demand, seasonality, platform
    /// benchmarks. Optional: a failure is recorded and the run continues.</summary>
    MarketResearch = 30,

    /// <summary>"What are these specific companies doing" — a different question from MarketResearch,
    /// with different queries and a different synthesis. Optional, same as above.</summary>
    CompetitorResearch = 40,

    /// <summary>Strategy part 1 of 3: business/market analysis, brand strategy, marketing strategy.</summary>
    StrategyPositioning = 50,

    /// <summary>Strategy part 2 of 3: the campaign blueprint (pillars, themes, cadence, content mix,
    /// platforms).</summary>
    StrategyBlueprint = 60,

    /// <summary>Strategy part 3 of 3: content production plan, execution roadmap, recommendations,
    /// executive summary.</summary>
    StrategyRoadmap = 70,

    /// <summary>Pure composition of the three strategy parts into the single user-facing artifact —
    /// no model call. Splitting generation into three small prompts means a failure retries one
    /// sub-task instead of losing the whole strategy.</summary>
    StrategyAssemble = 80,

    /// <summary>Blocks the run until a human approves. Refinement loops back through the strategy
    /// stages, producing a new artifact version rather than overwriting the current one.</summary>
    HumanApproval = 90,

    /// <summary>The post batch: executes the approved strategy into N draft ContentItems.</summary>
    ContentPlan = 100,

    /// <summary>One row per generated post (keyed by TargetRefId = ContentItemId), which is what
    /// makes per-post retry and bounded parallelism possible. Optional in the same sense as today:
    /// a permanent failure attaches the placeholder asset and the run continues, because a
    /// ContentItem must never end up with no image at all.</summary>
    ContentImage = 110
}

/// <summary>
/// Lifecycle of one execution of the pipeline. The two Awaiting* states are parked, not failed —
/// they resume without user-visible loss once the blocking condition clears.
/// </summary>
public enum AiPipelineRunStatus
{
    /// <summary>Created; no stage claimed yet.</summary>
    Pending,

    /// <summary>At least one stage is runnable or in flight.</summary>
    Running,

    /// <summary>Parked at HumanApproval. Resumes when the strategy is approved.</summary>
    AwaitingApproval,

    /// <summary>A stage needs coins the wallet doesn't have. Resumes on top-up. Distinct from Failed
    /// because nothing is wrong with the run — it is waiting on the tenant, and no attempt was
    /// consumed reaching this state.</summary>
    AwaitingCoins,

    /// <summary>Every stage reached a terminal state and no required stage failed.</summary>
    Completed,

    /// <summary>A required stage exhausted its attempts. Optional stages never cause this.</summary>
    Failed,

    /// <summary>Explicitly cancelled by a user.</summary>
    Cancelled
}

/// <summary>
/// Lifecycle of one stage of one run. Replaces the current design's inference of stage completion
/// from whether a JSON column on MarketingCampaign happens to be non-null — which cannot tell
/// "never ran" from "ran and failed" from "returned unparseable output" from "interrupted", four
/// situations needing four different recoveries.
/// </summary>
public enum AiPipelineStageStatus
{
    /// <summary>Not started, or retryable after a failure (see NextAttemptAt).</summary>
    Pending,

    /// <summary>Claimed by a worker and in flight. A crash leaves the row here until its lease
    /// expires, at which point it returns to Pending without consuming an attempt.</summary>
    Running,

    Completed,

    /// <summary>Attempts exhausted on a required stage. Fails the run.</summary>
    Failed,

    /// <summary>Attempts exhausted on an optional stage, or deliberately bypassed (e.g. a cached
    /// brand analysis). Satisfies dependencies exactly as Completed does, so the run continues.</summary>
    Skipped,

    /// <summary>HumanApproval only: waiting on a person, not on a provider.</summary>
    AwaitingApproval
}

/// <summary>
/// The typed outputs a stage can produce. Artifacts are versioned, so refining a strategy adds v2
/// rather than destroying v1 (today's RefineCampaignPlan overwrites AiPlanJson in place, with no
/// history and no way back to a version the user preferred).
/// </summary>
public enum AiArtifactKind
{
    BrandAnalysis = 10,
    CampaignAnalysis = 20,
    MarketResearch = 30,
    CompetitorResearch = 40,
    StrategyPositioning = 50,
    StrategyBlueprint = 60,
    StrategyRoadmap = 70,

    /// <summary>The assembled, user-facing strategy. Its JSON shape is deliberately identical to
    /// what MarketingCampaign.AiPlanJson holds today — the strategy review UI, campaign-strategy-page
    /// and the content prompt all read that shape.</summary>
    Strategy = 80,

    ContentPlan = 100
}

/// <summary>
/// Why a stage failed, which decides what to do about it. Retrying a malformed prompt three times
/// only burns money; retrying a provider timeout is usually free of charge and works.
/// </summary>
public enum AiFailureKind
{
    /// <summary>Anything unclassified. Treated as retryable.</summary>
    Unknown = 0,

    /// <summary>Transport-level: 5xx, timeout, circuit breaker. Retry with backoff.</summary>
    Provider = 1,

    /// <summary>The response was not valid JSON, or not the expected shape. Gets one "your previous
    /// response was not valid JSON" repair re-ask before being treated as a provider failure.</summary>
    Parse = 2,

    /// <summary>Provider key/organisation quota exhausted. Retryable, but with a long backoff and a
    /// distinct message — a shorter one would just burn the remaining keys.</summary>
    Quota = 3,

    /// <summary>The tenant's coin balance is short. Consumes no attempt and parks the run as
    /// AwaitingCoins; this is not a failure of the pipeline.</summary>
    Coins = 4,

    /// <summary>The artifact parsed but failed schema validation. One repair re-ask, then fail —
    /// re-running identical input will not produce a different shape.</summary>
    Validation = 5
}
