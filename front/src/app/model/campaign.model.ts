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
}

/** PUT /api/v1/campaigns/{id} request body. */
export interface UpdateCampaignInput {
  name?: string;
  status?: BackendCampaignStatus;
  startDate?: string;
  endDate?: string;
  budgetAmount?: number;
}
