import { BackendSocialPlatform } from './content-item.model';

export type CampaignStatus   = 'active' | 'paused' | 'completed' | 'draft' | 'archived';
export type CampaignPlatform = 'instagram' | 'facebook' | 'tiktok' | 'youtube' | 'x' | 'snapchat' | 'linkedin';
export type CampaignObjective = 'awareness' | 'traffic' | 'engagement' | 'leads' | 'sales';

/** Single source of truth for how a campaign renders, shared by the list card, the detail page,
 *  the campaign calendar and the post detail page — each of which used to carry its own private
 *  copy of these three maps, so a label fixed in one place stayed wrong in the other three. */
export interface CampaignPlatformMeta {
  icon: string;
  color: string;
  label: string;
}

export const CAMPAIGN_PLATFORM_META: Record<CampaignPlatform, CampaignPlatformMeta> = {
  instagram: { icon: 'fa-brands fa-instagram',   color: 'var(--color-instagram)', label: 'إنستغرام' },
  facebook:  { icon: 'fa-brands fa-facebook-f',  color: 'var(--color-facebook)',  label: 'فيسبوك' },
  tiktok:    { icon: 'fa-brands fa-tiktok',      color: 'var(--color-tiktok)',    label: 'تيك توك' },
  youtube:   { icon: 'fa-brands fa-youtube',     color: 'var(--color-youtube)',   label: 'يوتيوب' },
  x:         { icon: 'fa-brands fa-x-twitter',   color: 'var(--color-x)',         label: 'إكس' },
  snapchat:  { icon: 'fa-brands fa-snapchat',    color: 'var(--color-snapchat)',  label: 'سناب شات' },
  linkedin:  { icon: 'fa-brands fa-linkedin-in', color: 'var(--color-linkedin)',  label: 'لينكد إن' },
};

export const CAMPAIGN_STATUS_LABELS: Record<CampaignStatus, string> = {
  active: 'نشطة', paused: 'موقوفة', completed: 'مكتملة', draft: 'مسودة', archived: 'مؤرشفة',
};

export const CAMPAIGN_OBJECTIVE_LABELS: Record<CampaignObjective, string> = {
  awareness: 'الوعي بالعلامة', traffic: 'زيارات الموقع',
  engagement: 'التفاعل', leads: 'توليد عملاء', sales: 'رفع المبيعات',
};

/** `MarketingCampaign.Objective` is free text on the backend, so an objective that isn't one of
 *  the five known ones renders as the user's own words rather than as a blank (which is what the
 *  raw `Record` lookups used to produce) or as a fabricated "awareness". */
export function campaignObjectiveLabel(objective: string | null | undefined): string {
  if (!objective) return '—';
  return CAMPAIGN_OBJECTIVE_LABELS[objective.toLowerCase() as CampaignObjective] ?? objective;
}

export interface Campaign {
  id: string;
  name: string;
  /** The brand profile this campaign belongs to — every campaign is scoped
   *  under a single brand profile umbrella (see BrandProfile). */
  brandProfileId: string;
  status: CampaignStatus;
  platforms: CampaignPlatform[];
  /** Free text server-side — kept raw here (see `campaignObjectiveLabel`) rather than coerced
   *  into the typed union, so an unrecognised objective isn't silently shown as "awareness". */
  objective: string;
  industry?: string;
  budget?: number | null;
  budgetCurrency?: string | null;
  startDate: string;
  endDate: string;
  createdAt: string;
  /** Set once the AI strategy is approved — decides whether the card offers "review the
   *  strategy" or "review the content" as its next step. */
  planApprovedAt?: string | null;
  /** How many content items the campaign already has, so the list can tell a campaign with
   *  generated posts apart from one that still needs generating. */
  contentItemCount: number;
  adCount?: number;
  /** Per-campaign brand logo shown on the card banner. Falls back to the
   *  Rawaj logo (see CampaignCard) when not set. */
  logoUrl?: string;
}

/** Backend `CampaignStatus` enum values (PascalCase, as serialized by the API). */
export type BackendCampaignStatus = 'Draft' | 'Active' | 'Paused' | 'Completed' | 'Archived';

export const BACKEND_TO_CAMPAIGN_STATUS: Record<BackendCampaignStatus, CampaignStatus> = {
  Draft: 'draft',
  Active: 'active',
  Paused: 'paused',
  Completed: 'completed',
  Archived: 'archived',
};

/** Backend `SocialPlatform` enum names → the frontend's lowercase `CampaignPlatform`. The enum has
 *  no Snapchat member, so a campaign can never target it — anything unmapped is dropped by callers
 *  rather than guessed at. Lives here because CampaignService, ScheduledPostService, AdService and
 *  the campaign detail page all need the exact same mapping. */
export const BACKEND_TO_CAMPAIGN_PLATFORM: Record<string, CampaignPlatform> = {
  Instagram: 'instagram',
  Facebook: 'facebook',
  Tiktok: 'tiktok',
  Youtube: 'youtube',
  Twitter: 'x',
  Linkedin: 'linkedin',
};

/** GET /api/v1/campaigns — Rawaj.Application.Features.Campaigns.GetCampaigns.CampaignSummary */
export interface CampaignSummary {
  campaignId: string;
  brandProfileId: string;
  name: string;
  status: BackendCampaignStatus;
  startDate?: string | null;
  endDate?: string | null;
  createdAt: string;
  objective?: string | null;
  /** Backend SocialPlatform enum names (PascalCase), same as GetCampaignResponse.targetPlatforms. */
  targetPlatforms: string[];
  budgetAmount?: number | null;
  budgetCurrency?: string | null;
  /** Set once the AI strategy is approved — drives which step the list links the user to. */
  planApprovedAt?: string | null;
  contentItemCount: number;
}

/** POST /api/v1/campaigns — Rawaj.Application.Features.Campaigns.CreateCampaign.CreateCampaignResponse */
export interface CreateCampaignResponse {
  campaignId: string;
  brandProfileId: string;
  name: string;
  status: BackendCampaignStatus;
}

/** GET /api/v1/campaigns/{id}, PUT /api/v1/campaigns/{id} — GetCampaignResponse */
export interface GetCampaignResponse {
  campaignId: string;
  brandProfileId: string;
  name: string;
  objective?: string | null;
  targetPlatforms: string[];
  startDate?: string | null;
  endDate?: string | null;
  budgetAmount?: number | null;
  budgetCurrency?: string | null;
  status: BackendCampaignStatus;
  aiPlanJson?: string | null;
  aiGeneratedAt?: string | null;
  /** The onboarding wizard's collected answers, persisted server-side. */
  briefJson?: string | null;
  /** `{summary, competitors[], sources[], unavailable, note}` — best-effort Tavily research. */
  competitorResearchJson?: string | null;
  /** "What we understood about your business" AI artifact. */
  diagnosisJson?: string | null;
  /** Stamped once the user approves the generated strategy; gates content generation. */
  planApprovedAt?: string | null;
  createdAt: string;
  updatedAt: string;
}

/** POST /api/v1/campaigns request body. */
export interface CreateCampaignInput {
  brandProfileId: string;
  name: string;
  objective?: string;
  targetPlatforms: string[];
  startDate?: string;
  endDate?: string;
  budgetAmount?: number;
  budgetCurrency?: string;
  /** The onboarding wizard's raw collected-answers JSON blob. */
  briefJson?: string;
}

/** PUT /api/v1/campaigns/{id} request body. */
export interface UpdateCampaignInput {
  name?: string;
  status?: BackendCampaignStatus;
  startDate?: string;
  endDate?: string;
  budgetAmount?: number;
  objective?: string;
  targetPlatforms?: string[];
  budgetCurrency?: string;
  /** The onboarding wizard's raw collected-answers JSON blob, autosaved as the user progresses. */
  briefJson?: string;
}

/** POST /api/v1/campaigns/{id}/research-competitors — ResearchCampaignCompetitorsResponse.
 *  Succeeded reflects whether Tavily actually returned usable data — the endpoint itself never
 *  fails the flow, so a false here means "show an unavailable note", not an error. */
export interface ResearchCampaignCompetitorsResponse {
  campaignId: string;
  competitorResearchJson: string;
  succeeded: boolean;
  note?: string | null;
  researchedAt: string;
}

/** POST /api/v1/campaigns/{id}/diagnose-business — GenerateBusinessDiagnosisResponse */
export interface GenerateBusinessDiagnosisResponse {
  campaignId: string;
  diagnosisJson: string;
  diagnosedAt: string;
}

/** POST /api/v1/campaigns/{id}/refine-plan — RefineCampaignPlanResponse */
export interface RefineCampaignPlanResponse {
  campaignId: string;
  aiPlanJson: string;
  aiGeneratedAt: string;
}

/** POST /api/v1/campaigns/{id}/generate-plan — GenerateMarketingPlanResponse */
export interface GenerateMarketingPlanResponse {
  campaignId: string;
  aiPlanJson: string;
  aiGeneratedAt: string;
}

/** POST /api/v1/campaigns/{id}/unarchive — UnarchiveCampaignResponse. `status` is whichever side
 *  of the plan-approval gate the campaign was on when it was archived, so the caller knows which
 *  step it resumes at. */
export interface UnarchiveCampaignResponse {
  campaignId: string;
  status: BackendCampaignStatus;
}

/** POST /api/v1/campaigns/{id}/approve-plan — ApproveCampaignPlanResponse */
export interface ApproveCampaignPlanResponse {
  campaignId: string;
  status: BackendCampaignStatus;
  planApprovedAt: string;
}

/** POST /api/v1/campaigns/{id}/schedule-posts — ScheduleCampaignPostsResponse. Routes each approved
 *  content item to the brand's connected account matching that item's own platform — no
 *  socialAccountId is sent; items whose platform has no connected account come back `skipped`
 *  rather than `failed`, and never spend a coin. */
export interface ScheduleCampaignPostResult {
  contentItemId: string;
  platform: BackendSocialPlatform;
  succeeded: boolean;
  skipped: boolean;
  error?: string | null;
  scheduledPostId?: string | null;
  scheduledAt?: string | null;
}

export interface ScheduleCampaignPostsResponse {
  campaignId: string;
  succeededCount: number;
  failedCount: number;
  skippedCount: number;
  missingPlatforms: BackendSocialPlatform[];
  results: ScheduleCampaignPostResult[];
}

/** Parsed shape of GetCampaignResponse.aiPlanJson — the Complete Marketing Strategy pricing-sheet
 *  feature's output (see ContentPromptBuilder.BuildCampaignStrategyPrompt on the backend). */
export interface CampaignStrategy {
  executiveSummary?: string;
  businessAndMarketAnalysis?: string;
  brandStrategy?: string;
  marketingStrategy?: string;
  campaignBlueprint?: {
    pillars?: string[];
    keyThemes?: string[];
    postingCadence?: Record<string, string>;
    contentMix?: Record<string, string>;
    recommendedPlatforms?: string[];
  };
  contentProductionPlan?: string;
  executionRoadmap?: string;
  aiRecommendations?: string[];
}

/** Parsed shape of GetCampaignResponse.diagnosisJson — the AI Business Diagnosis feature's output. */
export interface BusinessDiagnosis {
  businessSummary?: string;
  swot?: { strengths?: string[]; weaknesses?: string[]; opportunities?: string[]; threats?: string[] };
  businessMaturity?: string;
  growthStage?: string;
  marketingReadiness?: string;
  currentRisks?: string[];
  opportunities?: string[];
  missingInformation?: string[];
}

/** Parsed shape of GetCampaignResponse.competitorResearchJson. */
export interface CompetitorResearch {
  summary?: string | null;
  competitors?: { name: string; url: string; snippet: string }[];
  sources?: { title: string; url: string }[];
  unavailable?: boolean;
  note?: string | null;
}
