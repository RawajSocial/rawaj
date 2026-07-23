import { Injectable, computed, inject, signal } from '@angular/core';
import { BrandProfileService } from './brand-profile.service';
import { CampaignService } from './campaign.service';
import { TenantService } from '../core/tenant/tenant.service';

const BRAND_KEY = 'rawaj.brandContext.brandProfileId';
const CAMPAIGN_KEY = 'rawaj.brandContext.campaignId';

/**
 * Global "which brand / which campaign am I looking at" selection, driven by
 * the header dropdowns and consumed by every brand-scoped page (Content
 * Generation's default, Ads, My Media, Marketing Plans, Calendar, Dashboard).
 * Persisted to localStorage so the selection survives reloads.
 */
@Injectable({ providedIn: 'root' })
export class BrandContextService {
  private readonly brandProfileService = inject(BrandProfileService);
  private readonly campaignService = inject(CampaignService);
  private readonly tenantService = inject(TenantService);

  private readonly _selectedBrandProfileId = signal<string | null>(this.readStored(BRAND_KEY));
  private readonly _selectedCampaignId = signal<string | 'all'>(this.readStored(CAMPAIGN_KEY) ?? 'all');

  readonly selectedBrandProfileId = this._selectedBrandProfileId.asReadonly();
  readonly selectedCampaignId = this._selectedCampaignId.asReadonly();

  readonly selectedBrandProfile = computed(() =>
    this.brandProfileService.profiles().find(p => p.id === this._selectedBrandProfileId()),
  );

  readonly campaignsForSelectedBrand = computed(() => {
    const bp = this._selectedBrandProfileId();
    return bp ? this.campaignService.byBrandProfile(bp)() : [];
  });

  setBrandProfile(id: string): void {
    this._selectedBrandProfileId.set(id);
    this._selectedCampaignId.set('all'); // reset on brand change
    this.writeStored(BRAND_KEY, id);
    this.writeStored(CAMPAIGN_KEY, 'all');
  }

  setCampaign(id: string | 'all'): void {
    this._selectedCampaignId.set(id);
    this.writeStored(CAMPAIGN_KEY, id);
  }

  /** Picks a default brand (tenant default, or first available) — only when nothing is selected yet. */
  initDefault(): void {
    if (this._selectedBrandProfileId()) return;
    const def = this.tenantService.defaultBrandProfileId() ?? this.brandProfileService.profiles()[0]?.id;
    if (def) this.setBrandProfile(def);
  }

  private readStored(key: string): string | null {
    try { return localStorage.getItem(key); } catch { return null; }
  }

  private writeStored(key: string, value: string): void {
    try { localStorage.setItem(key, value); } catch { /* noop */ }
  }
}
