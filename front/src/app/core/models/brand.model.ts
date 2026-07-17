import { BrandProfileStatus, BrandVoice } from './enums';

export interface BrandProfileSummary {
  brandProfileId: string;
  name: string;
  description: string | null;
  brandVoice: BrandVoice | null;
  status: BrandProfileStatus;
  isDefault: boolean;
}

export interface CreateBrandProfileRequest {
  name: string;
  description?: string | null;
  brandVoice?: BrandVoice | null;
  tagline?: string | null;
  industry?: string | null;
  targetAudience?: string | null;
  colors?: string[] | null;
  logoUrl?: string | null;
  websiteUrl?: string | null;
  supportedLanguages?: string[] | null;
  keywords?: string[] | null;
}

export interface CreateBrandProfileResponse {
  brandProfileId: string;
  tenantId: string;
  name: string;
  status: BrandProfileStatus;
  isDefault: boolean;
}

/** All fields optional - only fields present are applied server-side, the rest are left as-is. */
export type UpdateBrandProfileRequest = Partial<CreateBrandProfileRequest>;

export interface UpdateBrandProfileResponse {
  brandProfileId: string;
  name: string;
  description: string | null;
  brandVoice: BrandVoice | null;
  status: BrandProfileStatus;
  isDefault: boolean;
}

export interface GenerateOnboardingQuestionsResponse {
  questionsJson: string;
}
