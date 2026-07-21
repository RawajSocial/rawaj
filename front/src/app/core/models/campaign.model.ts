import { CampaignStatus, ContentTemplateStyle, Language } from './enums';

export interface CampaignSummary {
  campaignId: string;
  brandProfileId: string;
  name: string;
  status: CampaignStatus;
  startDate: string | null;
  endDate: string | null;
  createdAt: string;
}

export interface CampaignDetail {
  campaignId: string;
  brandProfileId: string;
  name: string;
  objective: string | null;
  targetPlatforms: string[];
  startDate: string | null;
  endDate: string | null;
  budgetAmount: number | null;
  budgetCurrency: string | null;
  status: CampaignStatus;
  aiPlanJson: string | null;
  aiGeneratedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCampaignRequest {
  brandProfileId: string;
  name: string;
  objective?: string | null;
  targetPlatforms: string[];
  startDate?: string | null;
  endDate?: string | null;
  budgetAmount?: number | null;
  budgetCurrency?: string | null;
}

export interface CreateCampaignResponse {
  campaignId: string;
  brandProfileId: string;
  name: string;
  status: CampaignStatus;
}

export interface GenerateMarketingPlanResponse {
  campaignId: string;
  aiPlanJson: string;
  aiGeneratedAt: string;
}

export interface GenerateCampaignContentRequest {
  postCount: number;
  language: Language;
  includeImages: boolean;
  templateStyle: ContentTemplateStyle;
}

export interface GenerateCampaignContentResponse {
  campaignId: string;
  generatedCount: number;
  imagesGenerated: number;
  imagesSkippedForCredits: number;
}

// Shape the AI is asked to return for aiPlanJson - see
// ContentPromptBuilder.BuildMarketingPlanPrompt on the backend. Parse defensively; the model's
// output is advisory content, not a backend-validated contract.
export interface MarketingPlan {
  pillars?: string[];
  keyThemes?: string[];
  postingCadence?: Record<string, string>;
  contentMix?: Record<string, string>;
  recommendedPlatforms?: string[];
  summary?: string;
}
