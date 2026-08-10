/** Backend `AiPipelineRunStatus` enum values — see docs/AI_PIPELINE.md §5. The two `Awaiting*`
 *  values are parked, not failed: a run resumes from either without user-visible loss once the
 *  blocking condition (a person, a wallet) clears. */
export type AiPipelineRunStatus =
  | 'Pending' | 'Running' | 'AwaitingApproval' | 'AwaitingCoins' | 'Completed' | 'Failed' | 'Cancelled';

/** A run has reached a status `AdvanceAsync` can never move past on its own — nothing left to poll
 *  for until a person acts (approve, top up, retry, start again). */
export const TERMINAL_RUN_STATUSES: ReadonlySet<AiPipelineRunStatus> =
  new Set<AiPipelineRunStatus>(['Completed', 'Failed', 'Cancelled']);

/** Backend `AiPipelineStageStatus` enum values. */
export type AiPipelineStageStatus = 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Skipped' | 'AwaitingApproval';

/** Backend `AiPipelineStageKind` enum values, in graph order — see docs/AI_PIPELINE.md §3. */
export type AiPipelineStageKind =
  | 'BrandAnalysis' | 'CampaignAnalysis' | 'MarketResearch' | 'CompetitorResearch'
  | 'StrategyPositioning' | 'StrategyBlueprint' | 'StrategyRoadmap' | 'StrategyAssemble'
  | 'HumanApproval' | 'ContentPlan' | 'ContentImage';

/** Backend `AiArtifactKind` enum values. `Strategy` is the assembled, user-facing one — its
 *  `contentJson` parses into the same `CampaignStrategy` shape already defined in
 *  `campaign.model.ts` (see docs/AI_PIPELINE.md §4: shape parity with `aiPlanJson` is a hard
 *  requirement, not a nicety). The others are advisory/intermediate and have no parsed type here
 *  yet — nothing reads them individually before C21/C22. */
export type AiArtifactKind =
  | 'BrandAnalysis' | 'CampaignAnalysis' | 'MarketResearch' | 'CompetitorResearch'
  | 'StrategyPositioning' | 'StrategyBlueprint' | 'StrategyRoadmap' | 'Strategy' | 'ContentPlan';

/** POST /api/v1/ai-pipeline/campaigns/{campaignId}/start — StartRunResponse */
export interface StartRunResponse {
  runId: string;
  status: AiPipelineRunStatus;
}

/** Server-computed progress — see `AiPipelineProgressPolicy` on the backend. Replaces the client's
 *  simulated `2500 + postCount * 1600`ms-toward-a-92%-cap timer (C22). */
export interface PipelineProgress {
  percent: number;
  completedStages: number;
  totalStages: number;
  /** Arabic label for whichever stage the run is currently on (or last acted on). */
  currentStageLabel: string;
  imagesCompleted: number;
  imagesTotal: number;
}

export interface StageStatusSummary {
  kind: AiPipelineStageKind;
  status: AiPipelineStageStatus;
  attempts: number;
  maxAttempts: number;
  lastError?: string | null;
  /** For a fanned-out stage (today, only `ContentImage`), the ContentItem id it's producing. Null
   *  for every other stage kind. Lets the client tell this run's batch of posts apart from ones the
   *  campaign already had from an earlier generation — ContentItem itself carries no batch id. */
  targetRefId?: string | null;
}

/** GET /api/v1/ai-pipeline/runs/{runId} — GetRunStatusResponse. The poll target.
 *
 *  `version`: a monotonic counter bumped on the backend every time a status snapshot is queued for
 *  push (see PipelineRunPublisher). Each ContentImage stage in a fanned-out batch completes in its own
 *  isolated DB scope and independently re-queries every sibling before pushing, so two pushes — one
 *  from a poll, one from SignalR, or two SignalR pushes themselves — can arrive out of order relative
 *  to which one actually reflects more progress. AiPipelineService compares this field and discards
 *  any incoming update whose version is lower than one it already has for the same run, rather than
 *  trusting delivery order. */
export interface GetRunStatusResponse {
  runId: string;
  status: AiPipelineRunStatus;
  progress: PipelineProgress;
  totalCoinsSpent: number;
  lastError?: string | null;
  stages: StageStatusSummary[];
  version: number;
}

/** GET /api/v1/ai-pipeline/campaigns/{campaignId}/artifacts/{kind} — GetArtifactResponse. Returns
 *  the *current* version only; fails if that kind hasn't been produced yet. */
export interface GetArtifactResponse {
  kind: AiArtifactKind;
  version: number;
  contentJson: string;
  createdAt: string;
}

/** POST /api/v1/ai-pipeline/runs/{runId}/stages/{kind}/run — RunStageResponse. Forces one stage to
 *  retry now, bypassing backoff; only from Pending/Failed/Skipped. */
export interface RunStageResponse {
  runId: string;
  runStatus: AiPipelineRunStatus;
  kind: AiPipelineStageKind;
  stageStatus: AiPipelineStageStatus;
}

/** POST /api/v1/ai-pipeline/runs/{runId}/resume — ResumeRunResponse */
export interface ResumeRunResponse {
  runId: string;
  status: AiPipelineRunStatus;
}

/** POST /api/v1/ai-pipeline/runs/{runId}/cancel — CancelRunResponse */
export interface CancelRunResponse {
  runId: string;
  status: AiPipelineRunStatus;
}

/** POST /api/v1/ai-pipeline/runs/{runId}/approve — ApproveStrategyResponse */
export interface ApproveStrategyResponse {
  runId: string;
  approvedArtifactId: string;
  approvedAt: string;
}

/** POST /api/v1/ai-pipeline/campaigns/{campaignId}/refine body */
export interface RefineStrategyInput {
  feedback: string;
}

/** POST /api/v1/ai-pipeline/campaigns/{campaignId}/refine — RefineStrategyResponse. Charges only on
 *  success — an unreadable model response leaves the previous strategy version current. */
export interface RefineStrategyResponse {
  artifactId: string;
  version: number;
  contentJson: string;
}
