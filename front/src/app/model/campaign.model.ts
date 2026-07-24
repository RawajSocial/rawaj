export type CampaignStatus   = 'active' | 'paused' | 'completed' | 'draft' | 'archived';
export type CampaignPlatform = 'instagram' | 'facebook' | 'tiktok' | 'youtube' | 'x' | 'snapchat' | 'linkedin';
export type CampaignObjective = 'awareness' | 'traffic' | 'engagement' | 'leads' | 'sales';

export interface Campaign {
  id: string;
  name: string;
  /** The brand profile this campaign belongs to — every campaign is scoped
   *  under a single brand profile umbrella (see BrandProfile). */
  brandProfileId: string;
  status: CampaignStatus;
  platforms: CampaignPlatform[];
  objective: CampaignObjective;
  industry?: string;
  budget: number;
  spent: number;
  reach: number;
  clicks: number;
  ctr: number;
  startDate: string;
  endDate: string;
  createdAt: string;
  adCount?: number;
  /** Per-campaign brand logo shown on the card banner. Falls back to the
   *  Rawaj logo (see CampaignCard) when not set. */
  logoUrl?: string;
}

/** Backend `CampaignStatus` enum values (PascalCase, as serialized by the API). */
export type BackendCampaignStatus = 'Draft' | 'Active' | 'Paused' | 'Completed' | 'Archived';

/** GET /api/v1/campaigns — Rawaj.Application.Features.Campaigns.GetCampaigns.CampaignSummary */
export interface CampaignSummary {
  campaignId: string;
  brandProfileId: string;
  name: string;
  status: BackendCampaignStatus;
  startDate?: string | null;
  endDate?: string | null;
  createdAt: string;
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

/** POST /api/v1/campaigns/{id}/approve-plan — ApproveCampaignPlanResponse */
export interface ApproveCampaignPlanResponse {
  campaignId: string;
  status: BackendCampaignStatus;
  planApprovedAt: string;
}

/** POST /api/v1/campaigns/{id}/schedule-posts — ScheduleCampaignPostsResponse */
export interface ScheduleCampaignPostResult {
  contentItemId: string;
  succeeded: boolean;
  error?: string | null;
  scheduledPostId?: string | null;
  scheduledAt?: string | null;
}

export interface ScheduleCampaignPostsResponse {
  campaignId: string;
  succeededCount: number;
  failedCount: number;
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
