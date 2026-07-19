import { Injectable, computed, signal } from '@angular/core';
import { Campaign, CampaignStatus } from '../model/campaign.model';

const MOCK_CAMPAIGNS: Campaign[] = [
  {
    id: '1',
    name: 'حملة رمضان الكريم ٢٠٢٥',
    brandProfileId: 'bp1',
    status: 'active',
    platforms: ['instagram', 'facebook', 'tiktok'],
    objective: 'sales',
    industry: 'retail',
    budget: 15000,
    spent: 9300,
    reach: 480000,
    clicks: 12400,
    ctr: 2.58,
    startDate: '٢٠٢٥/٠٣/٠١',
    endDate: '٢٠٢٥/٠٣/٣١',
    createdAt: '2025-02-20',
    adCount: 6,
    logoUrl: '/assets/icons/brand.png',
  },
  {
    id: '2',
    name: 'إطلاق منتج العيد',
    brandProfileId: 'bp2',
    status: 'paused',
    platforms: ['instagram', 'snapchat'],
    objective: 'awareness',
    budget: 8000,
    spent: 3200,
    reach: 220000,
    clicks: 5600,
    ctr: 2.54,
    startDate: '٢٠٢٥/٠٤/١٠',
    endDate: '٢٠٢٥/٠٤/٣٠',
    createdAt: '2025-04-05',
    adCount: 3,
    logoUrl: '/assets/icons/marketing.png',
  },
  {
    id: '3',
    name: 'حملة الصيف — التوعية',
    brandProfileId: 'bp1',
    status: 'draft',
    platforms: ['youtube', 'facebook'],
    objective: 'engagement',
    budget: 5000,
    spent: 0,
    reach: 0,
    clicks: 0,
    ctr: 0,
    startDate: '٢٠٢٥/٠٦/٠١',
    endDate: '٢٠٢٥/٠٨/٣١',
    createdAt: '2025-05-15',
    adCount: 0,
  },
];

@Injectable({ providedIn: 'root' })
export class CampaignService {
  private readonly _campaigns = signal<Campaign[]>(MOCK_CAMPAIGNS);
  readonly campaigns = this._campaigns.asReadonly();

  getById(id: string) {
    return computed(() => this._campaigns().find(c => c.id === id));
  }

  byBrandProfile(brandProfileId: string) {
    return computed(() => this._campaigns().filter(c => c.brandProfileId === brandProfileId));
  }

  pause(id: string): void {
    this._campaigns.update(list =>
      list.map(c => (c.id === id ? { ...c, status: 'paused' as CampaignStatus } : c)),
    );
  }

  resume(id: string): void {
    this._campaigns.update(list =>
      list.map(c => (c.id === id ? { ...c, status: 'active' as CampaignStatus } : c)),
    );
  }
}
