export type BrandVoice =
  | 'professional' | 'friendly' | 'bold' | 'playful' | 'elegant'
  | 'inspiring' | 'educational' | 'innovative' | 'motivating' | 'wittyFunny';

export type BrandProfileStatus = 'active' | 'draft' | 'archived';

/** Brand-level facts that used to be re-asked in the onboarding wizard's now-removed "Brand
 *  Overview" step (step 3) and parts of step 5 — collected once here instead, since they don't
 *  change per campaign. Mirrors the new fields on the backend's BrandInfo value object. */
export interface BrandProfileIdentity {
  instagram?: string;
  /** Bucketed range (e.g. "1 - 3 سنوات") — kept distinct from businessEstablishDate, which is an
   *  exact date; either can be present without the other. */
  businessAge?: string;
  businessEstablishDate?: string;
  stage?: string;
  uniqueValue?: string;
  pricePositioning?: string;
  storePresence?: string;
  existingPlatforms: string[];
  admiredBrand1?: string;
  admiredBrand2?: string;
  admiredBrand3?: string;
}

export interface BrandProfile extends BrandProfileIdentity {
  id: string;
  tenantId?: string;
  name: string;
  description?: string;
  tones: BrandVoice[];
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
  tones: BrandVoice[];
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
export interface BrandProfileDetail extends BrandProfileIdentity {
  brandProfileId: string;
  name: string;
  description?: string;
  tones: BrandVoice[];
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
  location?: string;
}

/** PUT /api/v1/brand-profiles/{id} — Rawaj.Application.Features.Brands.UpdateBrandProfile.UpdateBrandProfileCommand.
 *  Partial update: omitted/undefined fields are left unchanged server-side. Requires Editor role
 *  and — for Editor/Viewer — a TenantMemberBrandAccess row scoping the member to this brand. */
export interface UpdateBrandProfileRequest extends Partial<BrandProfileIdentity> {
  name?: string;
  description?: string;
  tones?: BrandVoice[];
  tagline?: string;
  industry?: string;
  targetAudience?: string;
  colors?: string[];
  logoUrl?: string;
  websiteUrl?: string;
  supportedLanguages?: string[];
  keywords?: string[];
  location?: string;
}

/** Rawaj.Application.Features.Brands.UpdateBrandProfile.UpdateBrandProfileResponse — a smaller
 *  echo than BrandProfileDetail; callers re-fetch getDetail() for the full record after saving. */
export interface UpdateBrandProfileResponse {
  brandProfileId: string;
  name: string;
  description?: string;
  tones: BrandVoice[];
  status: BrandProfileStatus;
  isDefault: boolean;
}

export const BRAND_VOICE_LABELS: Record<BrandVoice, string> = {
  professional: 'احترافي',
  friendly: 'ودود',
  bold: 'جريء',
  playful: 'مرح',
  elegant: 'أنيق',
  inspiring: 'ملهم',
  educational: 'تثقيفي',
  innovative: 'مبتكر',
  motivating: 'محفز',
  wittyFunny: 'ذكي ومضحك',
};

export const BRAND_PROFILE_STATUS_LABELS: Record<BrandProfileStatus, string> = {
  active: 'نشط',
  draft: 'مسودة',
  archived: 'مؤرشف',
};
