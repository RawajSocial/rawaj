import { Injectable, computed, signal } from '@angular/core';
import { BrandProfile } from '../model/brand-profile.model';

const MOCK_BRAND_PROFILES: BrandProfile[] = [
  {
    id: 'bp1',
    tenantId: 't1',
    name: 'رواج للتسويق الرقمي',
    description: 'الهوية الرئيسية لوكالة رواج وعملائها في قطاع التجزئة.',
    brandVoice: 'professional',
    status: 'active',
    tagline: 'نمو أذكى لعلامتك التجارية',
    industry: 'retail',
    targetAudience: 'الشباب والعائلات في الشرق الأوسط',
    colors: ['#7C3AED', '#2563EB'],
    logoUrl: '/assets/icons/brand.png',
    websiteUrl: 'https://rawaj.app',
    supportedLanguages: ['ar', 'en'],
    keywords: ['تسويق رقمي', 'إعلانات', 'رمضان'],
    isDefault: true,
    createdAt: '2025-01-15T10:00:00',
    updatedAt: '2025-06-01T10:00:00',
  },
  {
    id: 'bp2',
    tenantId: 't1',
    name: 'عطور الشرق',
    description: 'هوية علامة العطور الفاخرة الخاصة بالعميل.',
    brandVoice: 'luxurious',
    status: 'active',
    tagline: 'عبق الأصالة',
    industry: 'beauty',
    targetAudience: 'محبي العطور الفاخرة',
    colors: ['#B45309', '#1F2937'],
    websiteUrl: 'https://perfumes-example.com',
    supportedLanguages: ['ar'],
    keywords: ['عطور', 'فخامة', 'هدايا'],
    isDefault: false,
    createdAt: '2025-03-20T10:00:00',
    updatedAt: '2025-04-20T10:00:00',
  },
];

export interface CreateBrandProfileInput {
  name: string;
  description?: string;
  brandVoice?: BrandProfile['brandVoice'];
  tagline?: string;
  industry?: string;
  targetAudience?: string;
  colors?: string[];
  logoUrl?: string;
  websiteUrl?: string;
  supportedLanguages?: string[];
  keywords?: string[];
}

/**
 * Mock-data mirror of the backend's `POST /api/v1/tenants/brand-profile`
 * endpoint (see `TenantBrandProfileResponse` / `CreateBrandProfileCommand`
 * on the server) — same field shape, so swapping this for a real HttpClient
 * call later is a drop-in change.
 */
@Injectable({ providedIn: 'root' })
export class BrandProfileService {
  private readonly _profiles = signal<BrandProfile[]>(MOCK_BRAND_PROFILES);
  readonly profiles = this._profiles.asReadonly();

  getById(id: string) {
    return computed(() => this._profiles().find(p => p.id === id));
  }

  create(input: CreateBrandProfileInput): BrandProfile {
    const now = new Date().toISOString();
    const profile: BrandProfile = {
      id: 'bp' + Date.now(),
      tenantId: 't1',
      status: 'active',
      colors: input.colors ?? [],
      supportedLanguages: input.supportedLanguages ?? ['ar'],
      keywords: input.keywords ?? [],
      isDefault: this._profiles().length === 0,
      createdAt: now,
      updatedAt: now,
      ...input,
    };
    this._profiles.update(list => [profile, ...list]);
    return profile;
  }

  archive(id: string): void {
    this._profiles.update(list => list.map(p => (p.id === id ? { ...p, status: 'archived' } : p)));
  }
}
