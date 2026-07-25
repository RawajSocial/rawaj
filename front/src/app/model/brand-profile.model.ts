export type BrandVoice = 'professional' | 'friendly' | 'bold' | 'playful' | 'luxurious';

export type BrandProfileStatus = 'active' | 'draft' | 'archived';

export interface BrandProfile {
  id: string;
  tenantId?: string;
  name: string;
  description?: string;
  brandVoice?: BrandVoice;
  status: BrandProfileStatus;
  tagline?: string;
  industry?: string;
  targetAudience?: string;
  colors: string[];
  logoUrl?: string;
  websiteUrl?: string;
  supportedLanguages: string[];
  keywords: string[];
  location?: string;
  isDefault: boolean;
  createdAt?: string;
  updatedAt?: string;
}

/** GET /api/v1/brand-profiles — Rawaj.Application.Features.Brands.GetBrandProfiles.BrandProfileSummary */
export interface BrandProfileSummary {
  brandProfileId: string;
  name: string;
  description?: string;
  brandVoice?: BrandVoice;
  status: BrandProfileStatus;
  isDefault: boolean;
  tagline?: string;
  industry?: string;
  colors: string[];
  logoUrl?: string;
}

/** POST /api/v1/brand-profiles — Rawaj.Application.Features.Brands.CreateBrandProfile.CreateBrandProfileResponse */
export interface CreateBrandProfileResponse {
  brandProfileId: string;
  tenantId: string;
  name: string;
  status: BrandProfileStatus;
  isDefault: boolean;
}

/** GET /api/v1/brand-profiles/{id} — Rawaj.Application.Features.Brands.GetBrandProfile.GetBrandProfileResponse.
 *  The full record (unlike BrandProfileSummary, includes targetAudience/websiteUrl/supportedLanguages/keywords). */
export interface BrandProfileDetail {
  brandProfileId: string;
  name: string;
  description?: string;
  brandVoice?: BrandVoice;
  status: BrandProfileStatus;
  isDefault: boolean;
  tagline?: string;
  industry?: string;
  targetAudience?: string;
  colors: string[];
  logoUrl?: string;
  websiteUrl?: string;
  supportedLanguages: string[];
  keywords: string[];
}

/** PUT /api/v1/brand-profiles/{id} — Rawaj.Application.Features.Brands.UpdateBrandProfile.UpdateBrandProfileCommand.
 *  Partial update: omitted/undefined fields are left unchanged server-side. Requires Editor role
 *  and — for Editor/Viewer — a TenantMemberBrandAccess row scoping the member to this brand. */
export interface UpdateBrandProfileRequest {
  name?: string;
  description?: string;
  brandVoice?: BrandVoice;
  tagline?: string;
  industry?: string;
  targetAudience?: string;
  colors?: string[];
  logoUrl?: string;
  websiteUrl?: string;
  supportedLanguages?: string[];
  keywords?: string[];
}

/** Rawaj.Application.Features.Brands.UpdateBrandProfile.UpdateBrandProfileResponse — a smaller
 *  echo than BrandProfileDetail; callers re-fetch getDetail() for the full record after saving. */
export interface UpdateBrandProfileResponse {
  brandProfileId: string;
  name: string;
  description?: string;
  brandVoice?: BrandVoice;
  status: BrandProfileStatus;
  isDefault: boolean;
}

export const BRAND_VOICE_LABELS: Record<BrandVoice, string> = {
  professional: 'احترافي',
  friendly: 'ودود',
  bold: 'جريء',
  playful: 'مرح',
  luxurious: 'فاخر',
};

export const BRAND_PROFILE_STATUS_LABELS: Record<BrandProfileStatus, string> = {
  active: 'نشط',
  draft: 'مسودة',
  archived: 'مؤرشف',
};
