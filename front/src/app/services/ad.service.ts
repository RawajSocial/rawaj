import { Injectable, computed, signal } from '@angular/core';
import { Ad, AdStatus } from '../model/ad.model';

const MOCK_ADS: Ad[] = [
  { id: 'a1', name: 'إعلان الشريحة الرئيسية — رمضان',   campaignId: '1', campaignName: 'حملة رمضان الكريم ٢٠٢٥',   platforms: ['instagram', 'facebook'], status: 'active',    format: 'carousel', impressions: 184000, clicks: 4700,  ctr: 2.55, spend: 3200, cpc: 0.68, createdAt: '2025-03-01T08:30:00', postUrl: 'https://instagram.com/p/rawaj-a1' },
  { id: 'a2', name: 'فيديو قصير — منتج العيد',            campaignId: '1', campaignName: 'حملة رمضان الكريم ٢٠٢٥',   platforms: ['tiktok', 'instagram'],   status: 'active',    format: 'reel',     impressions: 210000, clicks: 5800,  ctr: 2.76, spend: 2800, cpc: 0.48, createdAt: '2025-03-05T16:00:00', postUrl: 'https://tiktok.com/@rawaj/video/a2' },
  { id: 'a3', name: 'منشور نصي — خصم ٣٠٪',                campaignId: '1', campaignName: 'حملة رمضان الكريم ٢٠٢٥',   platforms: ['facebook'],              status: 'paused',    format: 'text',     impressions: 86000,  clicks: 1900,  ctr: 2.21, spend: 1300, cpc: 0.68, createdAt: '2025-03-10T09:00:00', postUrl: 'https://facebook.com/rawaj/posts/a3' },
  { id: 'a4', name: 'إعلان منتج العيد — فيسبوك',          campaignId: '2', campaignName: 'إطلاق منتج العيد',          platforms: ['facebook', 'instagram', 'linkedin'], status: 'paused', format: 'image',    impressions: 120000, clicks: 3100,  ctr: 2.58, spend: 1800, cpc: 0.58, createdAt: '2025-04-10T14:00:00', imageUrl: '/ads/Product 1.jpg', postUrl: 'https://facebook.com/rawaj/posts/a4' },
  { id: 'a5', name: 'فيديو تعريفي — سناب شات',            campaignId: '2', campaignName: 'إطلاق منتج العيد',          platforms: ['snapchat'],              status: 'pending',   format: 'video',    impressions: 100000, clicks: 2500,  ctr: 2.50, spend: 1400, cpc: 0.56, createdAt: '2025-04-12T11:00:00' },
  { id: 'a6', name: 'إعلان الوعي — يوتيوب',               campaignId: '3', campaignName: 'حملة الصيف — التوعية',      platforms: ['youtube'],               status: 'pending',   format: 'video',    impressions: 0,      clicks: 0,     ctr: 0,    spend: 0,    cpc: 0,    createdAt: '2025-05-15T10:00:00' },
  { id: 'a7', name: 'صورة العطر الجديد',                   campaignId: '2', campaignName: 'إطلاق منتج العيد',          platforms: ['instagram', 'x'],        status: 'completed', format: 'image',    impressions: 96000,  clicks: 2600,  ctr: 2.71, spend: 1550, cpc: 0.60, createdAt: '2025-04-20T13:00:00', imageUrl: '/ads/perfume.jpeg', postUrl: 'https://instagram.com/p/rawaj-a7' },
];

@Injectable({ providedIn: 'root' })
export class AdService {
  private readonly _ads = signal<Ad[]>(MOCK_ADS);
  readonly ads = this._ads.asReadonly();

  getById(id: string) {
    return computed(() => this._ads().find(a => a.id === id));
  }

  toggle(id: string): void {
    this._ads.update(list =>
      list.map(a => (a.id === id ? { ...a, status: (a.status === 'active' ? 'paused' : 'active') as AdStatus } : a)),
    );
  }
}
