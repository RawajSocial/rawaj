export type BrandVoice = 'professional' | 'friendly' | 'bold' | 'playful' | 'luxurious';

export type BrandProfileStatus = 'active' | 'draft' | 'archived';

export interface BrandProfile {
  id: string;
  tenantId: string;
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
  createdAt: string;
  updatedAt: string;
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
